namespace AgentInfrastructure.Entities;

public class CodeEmbedding
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }

    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }

    /// <summary>class, method, block (fallback window)</summary>
    public string ChunkType { get; set; } = string.Empty;

    /// <summary>Optional symbol name, e.g. "OrderService.Create"</summary>
    public string? SymbolName { get; set; }

    /// <summary>Raw source text of the chunk (used to preview results)</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>float[] serialized little-endian: 4 bytes * Dim</summary>
    public byte[] Vector { get; set; } = Array.Empty<byte>();

    public int Dim { get; set; }

    /// <summary>sha256 of Content — dùng để skip re-embed khi file chưa đổi</summary>
    public string ContentHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Project Project { get; set; } = null!;
}
