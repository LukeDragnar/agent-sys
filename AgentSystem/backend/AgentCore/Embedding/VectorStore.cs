using AgentInfrastructure.Data;
using AgentInfrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentCore.Embedding;

/// <summary>
/// Persist + query vector qua SqlServer (AgentDbContext).
/// Vector lưu dạng varbinary (float[] little-endian).
/// Cosine similarity tính trong memory sau khi load về — OK với ~50k chunks.
/// </summary>
public class VectorStore
{
    private readonly AgentDbContext _db;

    public VectorStore(AgentDbContext db) { _db = db; }

    public async Task ClearProjectAsync(Guid projectId)
    {
        var rows = _db.Set<CodeEmbedding>().Where(x => x.ProjectId == projectId);
        _db.RemoveRange(rows);
        await _db.SaveChangesAsync();
    }

    public async Task<HashSet<string>> GetExistingHashesAsync(Guid projectId)
    {
        return (await _db.Set<CodeEmbedding>()
            .Where(x => x.ProjectId == projectId)
            .Select(x => x.ContentHash)
            .ToListAsync()).ToHashSet();
    }

    public async Task InsertBatchAsync(Guid projectId, IEnumerable<(CodeChunk chunk, float[] vector)> batch)
    {
        var now = DateTime.UtcNow;
        var entities = batch.Select(b => new CodeEmbedding
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            FilePath = b.chunk.FilePath,
            StartLine = b.chunk.StartLine,
            EndLine = b.chunk.EndLine,
            ChunkType = b.chunk.ChunkType,
            SymbolName = b.chunk.SymbolName,
            Content = b.chunk.Content.Length > 4000 ? b.chunk.Content.Substring(0, 4000) : b.chunk.Content,
            Vector = FloatToBytes(b.vector),
            Dim = b.vector.Length,
            ContentHash = b.chunk.ContentHash,
            CreatedAt = now,
        }).ToList();

        await _db.Set<CodeEmbedding>().AddRangeAsync(entities);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteByFileAsync(Guid projectId, string relativeFilePath)
    {
        var rows = _db.Set<CodeEmbedding>()
            .Where(x => x.ProjectId == projectId && x.FilePath == relativeFilePath);
        _db.RemoveRange(rows);
        await _db.SaveChangesAsync();
    }

    /// <summary>Load tất cả vector của project, tính cosine với query vector, trả top-K.</summary>
    public async Task<List<(CodeEmbedding row, double score)>> TopKAsync(Guid projectId, float[] queryVec, int k)
    {
        var rows = await _db.Set<CodeEmbedding>()
            .Where(x => x.ProjectId == projectId)
            .ToListAsync();

        var scored = new List<(CodeEmbedding row, double score)>(rows.Count);
        foreach (var row in rows)
        {
            var vec = BytesToFloat(row.Vector);
            scored.Add((row, CosineSimilarity(queryVec, vec)));
        }

        return scored.OrderByDescending(x => x.score).Take(k).ToList();
    }

    // ── helpers ──

    private static byte[] FloatToBytes(float[] vec)
    {
        var bytes = new byte[vec.Length * 4];
        Buffer.BlockCopy(vec, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] BytesToFloat(byte[] bytes)
    {
        var vec = new float[bytes.Length / 4];
        Buffer.BlockCopy(bytes, 0, vec, 0, bytes.Length);
        return vec;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        // Cả 2 đã L2-normalized → cosine = dot product
        if (a.Length != b.Length) return 0;
        double dot = 0;
        for (int i = 0; i < a.Length; i++) dot += a[i] * b[i];
        return dot;
    }
}
