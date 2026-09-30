namespace AgentInfrastructure.Entities;

public class CodeApiEndpoint
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }

    /// <summary>GET, POST, PUT, DELETE, PATCH</summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>Full route template: "api/order/{id}"</summary>
    public string RouteTemplate { get; set; } = string.Empty;

    public string ControllerName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;

    /// <summary>FK to the action method symbol</summary>
    public Guid? SymbolId { get; set; }

    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }

    public DateTime ScannedAt { get; set; }

    public Project Project { get; set; } = null!;
    public CodeSymbol? Symbol { get; set; }
}
