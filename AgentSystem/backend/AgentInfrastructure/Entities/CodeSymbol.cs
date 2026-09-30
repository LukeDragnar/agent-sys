namespace AgentInfrastructure.Entities;

public class CodeSymbol
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }

    /// <summary>Short name: "OrderService"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Fully qualified: "SCRM.Services.Order.OrderService"</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Class, Interface, Enum, Struct, Record, Method, Property, Field</summary>
    public string SymbolType { get; set; } = string.Empty;

    /// <summary>Relative path from project root</summary>
    public string FilePath { get; set; } = string.Empty;

    public int LineNumber { get; set; }

    public string Namespace { get; set; } = string.Empty;

    /// <summary>For methods/properties → owning class Id</summary>
    public Guid? ParentId { get; set; }

    /// <summary>public, private, internal, protected</summary>
    public string Accessibility { get; set; } = string.Empty;

    /// <summary>Return type for methods/properties</summary>
    public string? ReturnType { get; set; }

    /// <summary>For methods: "int id, string name"</summary>
    public string? Parameters { get; set; }

    /// <summary>Comma-separated base types: "IOrderService, BaseService"</summary>
    public string? BaseTypes { get; set; }

    /// <summary>
    /// JSON string with extra details:
    /// - Enum: [{"name":"BanHang","value":1,"description":"Bán hàng"}, ...]
    /// - Class/Method: [{"name":"Description","args":"\"Tạo đơn hàng\""}] (attributes)
    /// - Field: {"value":"100"} (const/static value)
    /// </summary>
    public string? ExtraInfo { get; set; }

    public DateTime ScannedAt { get; set; }

    public Project Project { get; set; } = null!;
    public CodeSymbol? Parent { get; set; }
    public List<CodeSymbol> Children { get; set; } = new();
}
