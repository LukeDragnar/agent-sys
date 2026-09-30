using Microsoft.Extensions.Logging;

namespace AgentCore.Embedding;

public class EmbeddingService : IEmbeddingService
{
    private const int EmbedBatchSize = 32;

    private readonly CodeChunker _chunker;
    private readonly OnnxEmbedder _embedder;
    private readonly VectorStore _store;
    private readonly ILogger<EmbeddingService> _logger;
    private static readonly Dictionary<Guid, EmbedIndexProgress> _progressMap = new();

    public EmbeddingService(
        CodeChunker chunker,
        OnnxEmbedder embedder,
        VectorStore store,
        ILogger<EmbeddingService> logger)
    {
        _chunker = chunker;
        _embedder = embedder;
        _store = store;
        _logger = logger;
    }

    public EmbedIndexProgress? GetProgress(Guid projectId)
    {
        lock (_progressMap) { return _progressMap.TryGetValue(projectId, out var p) ? p : null; }
    }

    public async Task<EmbedIndexStats> IndexProjectAsync(Guid projectId, string rootPath)
    {
        EnsureReady();
        await _store.ClearProjectAsync(projectId);
        return await IndexInternalAsync(projectId, rootPath, incremental: false);
    }

    public async Task<EmbedIndexStats> IndexIncrementalAsync(Guid projectId, string rootPath)
    {
        EnsureReady();
        return await IndexInternalAsync(projectId, rootPath, incremental: true);
    }

    public Task ClearProjectAsync(Guid projectId) => _store.ClearProjectAsync(projectId);

    public async Task<List<EmbedSearchHit>> SearchAsync(Guid projectId, string query, int limit = 10)
    {
        EnsureReady();
        var queryVec = _embedder.Embed(new[] { query })[0];
        var top = await _store.TopKAsync(projectId, queryVec, limit);

        return top.Select(x => new EmbedSearchHit
        {
            FilePath = x.row.FilePath,
            StartLine = x.row.StartLine,
            EndLine = x.row.EndLine,
            ChunkType = x.row.ChunkType,
            SymbolName = x.row.SymbolName,
            Preview = Truncate(x.row.Content, 300),
            Score = Math.Round(x.score, 4),
        }).ToList();
    }

    // ── internal ──

    private async Task<EmbedIndexStats> IndexInternalAsync(Guid projectId, string rootPath, bool incremental)
    {
        var stats = new EmbedIndexStats
        {
            ProjectId = projectId,
            Dim = _embedder.Dim,
            StartedAt = DateTime.UtcNow,
        };

        var progress = new EmbedIndexProgress { IsRunning = true, Phase = "discovering" };
        lock (_progressMap) { _progressMap[projectId] = progress; }

        try
        {
            var files = _chunker.DiscoverFiles(rootPath).ToList();
            stats.TotalFiles = files.Count;
            progress.TotalFiles = files.Count;
            progress.Phase = "chunking";

            var existing = incremental
                ? await _store.GetExistingHashesAsync(projectId)
                : new HashSet<string>();

            var pending = new List<CodeChunk>();

            foreach (var file in files)
            {
                progress.ProcessedFiles++;
                try
                {
                    var chunks = _chunker.ChunkFile(file, rootPath);
                    stats.TotalChunks += chunks.Count;

                    foreach (var c in chunks)
                    {
                        if (existing.Contains(c.ContentHash))
                        {
                            stats.SkippedChunks++;
                            continue;
                        }
                        pending.Add(c);

                        if (pending.Count >= EmbedBatchSize)
                        {
                            await EmbedAndStoreBatch(projectId, pending);
                            stats.NewChunks += pending.Count;
                            pending.Clear();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to chunk {File}", file);
                }
            }

            if (pending.Count > 0)
            {
                await EmbedAndStoreBatch(projectId, pending);
                stats.NewChunks += pending.Count;
            }

            progress.Phase = "done";
            stats.FinishedAt = DateTime.UtcNow;
            _logger.LogInformation(
                "Embedding indexed project {Pid}: {New} new, {Skip} skipped, {Total} total chunks from {Files} files",
                projectId, stats.NewChunks, stats.SkippedChunks, stats.TotalChunks, stats.TotalFiles);
            return stats;
        }
        finally
        {
            progress.IsRunning = false;
        }
    }

    private async Task EmbedAndStoreBatch(Guid projectId, List<CodeChunk> batch)
    {
        var vectors = _embedder.Embed(batch.Select(b => b.Content).ToList());
        var pairs = batch.Zip(vectors, (c, v) => (c, v));
        await _store.InsertBatchAsync(projectId, pairs);
    }

    private void EnsureReady()
    {
        if (!_embedder.IsReady)
            throw new InvalidOperationException(
                "Embedding model chưa được load. Chạy `scripts/download-embedding-model.sh` rồi restart AgentService.");
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s.Substring(0, max) + "…";
}
