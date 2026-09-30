namespace AgentCore.Embedding;

// ── DTOs ──

public class EmbedIndexStats
{
    public Guid ProjectId { get; set; }
    public int TotalFiles { get; set; }
    public int TotalChunks { get; set; }
    public int NewChunks { get; set; }
    public int SkippedChunks { get; set; }
    public int Dim { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
}

public class EmbedSearchHit
{
    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string ChunkType { get; set; } = string.Empty;
    public string? SymbolName { get; set; }
    public string Preview { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class EmbedIndexProgress
{
    public bool IsRunning { get; set; }
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int Percent => TotalFiles > 0 ? (int)(ProcessedFiles * 100.0 / TotalFiles) : 0;
    public string Phase { get; set; } = string.Empty;
}

// ── Interface ──

public interface IEmbeddingService
{
    Task<EmbedIndexStats> IndexProjectAsync(Guid projectId, string rootPath);
    Task<EmbedIndexStats> IndexIncrementalAsync(Guid projectId, string rootPath);
    Task ClearProjectAsync(Guid projectId);
    EmbedIndexProgress? GetProgress(Guid projectId);

    Task<List<EmbedSearchHit>> SearchAsync(Guid projectId, string query, int limit = 10);
}
