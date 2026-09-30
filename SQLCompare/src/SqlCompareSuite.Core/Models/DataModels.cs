namespace SqlCompareSuite.Core.Models;

/// <summary>1 dòng dữ liệu đọc từ table, key theo primary key.</summary>
public sealed class DataRow
{
    /// <summary>Chuỗi key ghép từ các cột PK (dùng để match 2 bên).</summary>
    public string Key { get; set; } = "";

    /// <summary>Giá trị từng cột: tên cột -> giá trị (object hoặc DBNull).</summary>
    public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>1 dòng kết quả so sánh data.</summary>
public sealed class DataDiffRow
{
    public string Key { get; set; } = "";
    public DiffStatus Status { get; set; }

    public DataRow? SourceRow { get; set; }
    public DataRow? TargetRow { get; set; }

    /// <summary>Các cột khác nhau (chỉ khi Status = Different).</summary>
    public List<string> DifferentColumns { get; set; } = new();
}

/// <summary>Cách xác định key so sánh của 1 table.</summary>
public enum DataKeyKind
{
    PrimaryKey,   // dùng primary key thật
    UniqueIndex,  // fallback: cột của 1 unique index
    AllColumns,   // fallback cuối: dùng tất cả cột làm key (chỉ Insert/Delete, không Update)
}

/// <summary>Kết quả so sánh data của 1 table.</summary>
public sealed class DataComparisonResult
{
    public string TableKey { get; set; } = "";
    public List<string> KeyColumns { get; set; } = new();
    public List<string> AllColumns { get; set; } = new();
    public List<DataDiffRow> Rows { get; } = new();

    /// <summary>Loại key đã dùng để so sánh (PK / unique index / all-columns).</summary>
    public DataKeyKind KeyKind { get; set; } = DataKeyKind.PrimaryKey;

    /// <summary>
    /// True khi key = tất cả cột (table không có PK/unique index thật).
    /// Trường hợp này không phát hiện Different/UPDATE, chỉ Insert/Delete.
    /// </summary>
    public bool IsAllColumnsKey => KeyKind == DataKeyKind.AllColumns;

    /// <summary>
    /// Số Identical được đếm tường minh khi KHÔNG lưu Identical rows vào Rows
    /// (IncludeIdenticalRows = false). Null => đếm từ Rows.
    /// </summary>
    public int? ExplicitIdenticalCount { get; set; }

    public int CountOnlyInSource => Rows.Count(r => r.Status == DiffStatus.OnlyInSource);
    public int CountOnlyInTarget => Rows.Count(r => r.Status == DiffStatus.OnlyInTarget);
    public int CountDifferent => Rows.Count(r => r.Status == DiffStatus.Different);

    /// <summary>Ưu tiên ExplicitIdenticalCount (khi không lưu rows), ngược lại đếm từ Rows.</summary>
    public int CountIdentical => ExplicitIdenticalCount ?? Rows.Count(r => r.Status == DiffStatus.Identical);
}

/// <summary>
/// Thống kê COUNT-ONLY của 1 table (cho danh sách bảng kiểu Redgate) — KHÔNG giữ rows.
/// Nhẹ hơn nhiều so với <see cref="DataComparisonResult"/>: chỉ đọc đếm rồi bỏ, không build VM.
/// </summary>
public sealed class DataTableSummary
{
    public string TableKey { get; set; } = "";
    public int OnlyInSource { get; set; }
    public int Different { get; set; }
    public int OnlyInTarget { get; set; }
    public int Identical { get; set; }
    public DataKeyKind KeyKind { get; set; } = DataKeyKind.PrimaryKey;
    public List<string> KeyColumns { get; set; } = new();

    /// <summary>True nếu table không so được (thiếu cột/quyền/khác cấu trúc).</summary>
    public bool Failed { get; set; }
    public string? Error { get; set; }

    public int TotalDiff => OnlyInSource + Different + OnlyInTarget;
    public bool HasDifferences => TotalDiff > 0;
}

/// <summary>Tuỳ chọn so sánh data.</summary>
public sealed class DataCompareOptions
{
    /// <summary>Có lưu các row Identical vào result.Rows hay không (mặc định KHÔNG để tiết kiệm bộ nhớ).</summary>
    public bool IncludeIdenticalRows { get; set; } = false;

    /// <summary>Filter WHERE áp cho cả 2 bên (không gồm từ khoá WHERE). Null = không filter.</summary>
    public string? WhereClause { get; set; } = null;

    /// <summary>
    /// Cột key tự chọn để so sánh thay cho PK/unique index.
    /// Nếu có giá trị => dùng đúng các cột này làm key (vd dùng Email làm key) và hành xử
    /// như PrimaryKey (cho phép phát hiện Different/UPDATE). Các cột phải tồn tại trong table.
    /// Null/empty = dùng resolve mặc định (PK -> unique -> all-columns).
    /// </summary>
    public List<string>? CustomKeyColumns { get; set; } = null;

    /// <summary>
    /// Giới hạn số dòng đọc mỗi bên: khi set => dùng SELECT TOP (N) ... ORDER BY key cho cả 2 bên.
    /// Null = đọc tất cả.
    /// </summary>
    public int? TopN { get; set; } = null;
}
