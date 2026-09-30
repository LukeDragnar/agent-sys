using System.Collections.Generic;
using System.ComponentModel;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.App.Views;

/// <summary>Wrapper giữ trạng thái checkbox "Include" cho 1 dòng schema diff.</summary>
public sealed class SchemaRowVm : INotifyPropertyChanged
{
    private bool _include;

    public SchemaRowVm(SchemaDiffItem item)
    {
        Item = item;
        _include = false; // mặc định KHÔNG tick gì (user tự chọn)
    }

    public SchemaDiffItem Item { get; }

    public bool Include
    {
        get => _include;
        set
        {
            if (_include == value) return;
            _include = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Include)));
        }
    }

    public string TypeText => Item.TypeText;
    public string Name => Item.FullName;
    public string StatusText => Item.StatusText;
    public DiffStatus Status => Item.Status;
    public string Details => string.Join("; ", Item.Details);

    /// <summary>Icon nhỏ theo loại object (kiểu Redgate).</summary>
    public string TypeIcon => Item.ObjectType switch
    {
        DbObjectType.Table => "▦",
        DbObjectType.View => "▤",
        DbObjectType.StoredProcedure => "⚙",
        DbObjectType.Function => "ƒ",
        DbObjectType.Trigger => "⚡",
        DbObjectType.Sequence => "#",
        DbObjectType.Synonym => "↪",
        DbObjectType.Schema => "🗂",
        DbObjectType.User => "👤",
        DbObjectType.Role => "👥",
        DbObjectType.Permission => "🔑",
        DbObjectType.PartitionFunction => "⑂",
        DbObjectType.PartitionScheme => "▥",
        _ => "•",
    };

    /// <summary>Thời gian sửa cuối của object 2 bên (set lúc dựng row từ sys.objects.modify_date). "" nếu không có.</summary>
    public string SourceModifiedText { get; set; } = "";
    public string TargetModifiedText { get; set; } = "";

    /// <summary>Thứ tự nhóm: Different(0) → OnlyInSource(1) → OnlyInTarget(2) → Identical(3, dưới cùng).</summary>
    public int StatusOrder => Item.Status switch
    {
        DiffStatus.Different => 0,
        DiffStatus.OnlyInSource => 1,
        DiffStatus.OnlyInTarget => 2,
        _ => 3, // Identical
    };

    /// <summary>Identical (dấu =) → KHÔNG cho tick (không có gì để deploy).</summary>
    public System.Windows.Visibility IncludeCheckVisibility =>
        Item.Status == DiffStatus.Identical ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

    /// <summary>Mũi tên giữa 2 cột object name (chiều đồng bộ Target ← Source).</summary>
    public string Arrow => Item.Status switch
    {
        DiffStatus.OnlyInSource => "→",
        DiffStatus.OnlyInTarget => "←",
        DiffStatus.Different => "⇄",
        _ => "=",
    };

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>1 cell giá trị 2 bên cho grid Data Compare side-by-side (1 cột của 1 row).</summary>
public sealed class DataCellVm
{
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";

    /// <summary>True khi giá trị 2 bên khác nhau (cột thuộc DifferentColumns) → tô nền xanh.</summary>
    public bool Different { get; set; }
}

/// <summary>Wrapper giữ trạng thái checkbox "Include" cho 1 dòng data diff.</summary>
public sealed class DataRowVm : INotifyPropertyChanged
{
    private bool _include;

    public DataRowVm(DataDiffRow row)
    {
        Row = row;
        _include = true; // mặc định tick TẤT CẢ (select all)
    }

    /// <summary>
    /// Ctor align Cells theo allColumns: dùng khi bind cột động Cells[i].Source / Cells[i].Target.
    /// Cells build LAZY (chỉ khi lần đầu truy cập) → bảng cực to chỉ build cells cho row đang hiển thị.
    /// </summary>
    public DataRowVm(DataDiffRow row, IList<string> allColumns) : this(row)
    {
        _allColumns = allColumns;
    }

    private readonly IList<string>? _allColumns;
    private IList<DataCellVm>? _cells;

    /// <summary>
    /// Giá trị 2 bên align theo thứ tự allColumns. Build lazy lần đầu render (đỡ lag bảng to:
    /// row virtualization chỉ render ~vài chục row → chỉ build cells cho từng đó).
    /// </summary>
    public IList<DataCellVm> Cells => _cells ??= BuildCells();

    /// <summary>Dựng Cells từ SourceRow/TargetRow.Values + DifferentColumns, align theo allColumns.</summary>
    private IList<DataCellVm> BuildCells()
    {
        var list = new List<DataCellVm>();
        if (_allColumns == null) return list;

        var diffSet = new HashSet<string>(Row.DifferentColumns, System.StringComparer.OrdinalIgnoreCase);
        var status = Row.Status;
        foreach (var col in _allColumns)
        {
            string s = ReadCell(Row.SourceRow, col);
            string t = ReadCell(Row.TargetRow, col);
            // Đánh dấu: row Different → đúng cột khác; chỉ-source/chỉ-target → tô ô có giá trị (cả row).
            bool diff = status switch
            {
                DiffStatus.Different => diffSet.Contains(col),
                DiffStatus.OnlyInSource => s.Length > 0,
                DiffStatus.OnlyInTarget => t.Length > 0,
                _ => false, // Identical
            };
            list.Add(new DataCellVm { Source = s, Target = t, Different = diff });
        }
        return list;
    }

    /// <summary>Đọc giá trị 1 cột: null/DBNull → "&lt;NULL&gt;"; thiếu cột → "".</summary>
    private static string ReadCell(DataRow? row, string col)
    {
        if (row == null) return "";
        if (!row.Values.TryGetValue(col, out var val)) return "";
        if (val == null || val is System.DBNull) return "<NULL>";
        return val.ToString() ?? "<NULL>";
    }

    public DataDiffRow Row { get; }

    public bool Include
    {
        get => _include;
        set
        {
            if (_include == value) return;
            _include = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Include)));
        }
    }

    public string Key => Row.Key;
    public string StatusText => Row.Status switch
    {
        DiffStatus.OnlyInSource => "Only in Source",
        DiffStatus.OnlyInTarget => "Only in Target",
        DiffStatus.Different => "Different",
        _ => "Identical",
    };
    public DiffStatus Status => Row.Status;
    public string DifferentColumns => string.Join(", ", Row.DifferentColumns);

    public event PropertyChangedEventHandler? PropertyChanged;
}
