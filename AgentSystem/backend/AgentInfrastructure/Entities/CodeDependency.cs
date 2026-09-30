namespace AgentInfrastructure.Entities;

public class CodeDependency
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }

    /// <summary>The class that depends on something</summary>
    public Guid FromSymbolId { get; set; }

    /// <summary>Resolved FK if the target symbol is in our scan</summary>
    public Guid? ToSymbolId { get; set; }

    /// <summary>Always populated: "IOrderRepository", "ILogger&lt;T&gt;"</summary>
    public string ToSymbolName { get; set; } = string.Empty;

    /// <summary>Injection, Inheritance, Implementation</summary>
    public string DependencyType { get; set; } = string.Empty;

    public DateTime ScannedAt { get; set; }

    public Project Project { get; set; } = null!;
    public CodeSymbol FromSymbol { get; set; } = null!;
    public CodeSymbol? ToSymbol { get; set; }
}
