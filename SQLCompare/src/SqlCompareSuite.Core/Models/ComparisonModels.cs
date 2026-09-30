namespace SqlCompareSuite.Core.Models;

/// <summary>Trạng thái diff của 1 object/row giữa Source (left) và Target (right).</summary>
public enum DiffStatus
{
    OnlyInSource,   // chỉ có bên trái
    OnlyInTarget,   // chỉ có bên phải
    Different,      // có cả 2 bên nhưng khác nhau
    Identical,      // giống hệt
}

/// <summary>1 dòng kết quả so sánh schema (1 object).</summary>
public sealed class SchemaDiffItem
{
    public DbObjectType ObjectType { get; set; }
    public string Schema { get; set; } = "dbo";
    public string Name { get; set; } = "";
    public DiffStatus Status { get; set; }

    /// <summary>Mô tả chi tiết khác biệt (cột thêm/bớt/đổi kiểu...). Rỗng nếu Identical.</summary>
    public List<string> Details { get; set; } = new();

    public string FullName => $"[{Schema}].[{Name}]";
    public string Key => $"{Schema}.{Name}";
    public string TypeText => ObjectType.ToString();
    public string StatusText => Status switch
    {
        DiffStatus.OnlyInSource => "Only in Source",
        DiffStatus.OnlyInTarget => "Only in Target",
        DiffStatus.Different => "Different",
        _ => "Identical",
    };
}

/// <summary>Kết quả so sánh schema toàn bộ 2 database.</summary>
public sealed class SchemaComparisonResult
{
    public List<SchemaDiffItem> Items { get; } = new();

    public int CountOnlyInSource => Items.Count(i => i.Status == DiffStatus.OnlyInSource);
    public int CountOnlyInTarget => Items.Count(i => i.Status == DiffStatus.OnlyInTarget);
    public int CountDifferent => Items.Count(i => i.Status == DiffStatus.Different);
    public int CountIdentical => Items.Count(i => i.Status == DiffStatus.Identical);
}
