using AgentInfrastructure.Entities;

namespace AgentCore.CodeMap;

// ── Query DTOs ──

public class SymbolResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string SymbolType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string Accessibility { get; set; } = string.Empty;
    public string? ReturnType { get; set; }
    public string? Parameters { get; set; }
    public string? BaseTypes { get; set; }
    public string? ExtraInfo { get; set; }
    public List<SymbolResult> Children { get; set; } = new();
    public List<DependencyResult> Dependencies { get; set; } = new();
}

public class EndpointResult
{
    public string HttpMethod { get; set; } = string.Empty;
    public string RouteTemplate { get; set; } = string.Empty;
    public string ControllerName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public List<string> ServiceChain { get; set; } = new();
}

public class DependencyResult
{
    public string FromSymbol { get; set; } = string.Empty;
    public string ToSymbol { get; set; } = string.Empty;
    public string DependencyType { get; set; } = string.Empty;
}

public class DependencyTreeResult
{
    public string Symbol { get; set; } = string.Empty;
    public List<DependencyResult> DependsOn { get; set; } = new();
    public List<DependencyResult> UsedBy { get; set; } = new();
}

public class SearchResult
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string SymbolType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string? ExtraInfo { get; set; }
}

public class ScanStats
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int Classes { get; set; }
    public int Interfaces { get; set; }
    public int Methods { get; set; }
    public int Properties { get; set; }
    public int Endpoints { get; set; }
    public int Dependencies { get; set; }
    public DateTime? LastScannedAt { get; set; }
}

public class TreeNode
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // folder, file, class, method...
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
    public List<TreeNode> Children { get; set; } = new();
}

public class ScanProgress
{
    public bool IsRunning { get; set; }
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int Percent => TotalFiles > 0 ? (int)(ProcessedFiles * 100.0 / TotalFiles) : 0;
    public string Phase { get; set; } = string.Empty; // "discovering", "analyzing", "saving", "done"
}

public class GraphData
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
}

public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Class, Interface, Service, Controller, Repository...
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int MethodCount { get; set; }
    public int DependencyCount { get; set; }
}

public class GraphEdge
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Injection, Inheritance, Implementation
}

// ── Interface ──

public interface ICodeMapService
{
    // Scan
    Task<ScanStats> ScanProjectAsync(Guid projectId, string rootPath);
    Task<ScanStats> ScanIncrementalAsync(Guid projectId, string rootPath);
    Task<ScanStats> ScanAddPathAsync(Guid projectId, string rootPath);
    Task ClearProjectAsync(Guid projectId);
    ScanProgress? GetScanProgress(Guid projectId);

    // Queries
    Task<List<SymbolResult>> FindSymbolAsync(Guid projectId, string name, string? symbolType = null);
    Task<List<EndpointResult>> FindEndpointAsync(Guid projectId, string? route = null, string? httpMethod = null);
    Task<DependencyTreeResult> GetDependenciesAsync(Guid projectId, string symbolName, int depth = 1);
    Task<List<SearchResult>> SearchAsync(Guid projectId, string query, int limit = 20);

    // Stats & tree (for Web UI)
    Task<ScanStats> GetStatsAsync(Guid projectId);
    Task<List<TreeNode>> GetTreeAsync(Guid projectId);
    Task<GraphData> GetGraphAsync(Guid projectId, string? filter = null);
    Task<GraphData> GetModuleGraphAsync(Guid projectId);
}
