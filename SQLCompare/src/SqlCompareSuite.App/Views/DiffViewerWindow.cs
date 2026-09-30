using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.App.Views;

/// <summary>
/// Cửa sổ xem chi tiết side-by-side 1 diff: schema object (programmable/table)
/// hoặc 1 data row. Mở qua các static helper ShowSchema / ShowDataRow.
/// </summary>
public partial class DiffViewerWindow : Window
{
    public DiffViewerWindow()
    {
        InitializeComponent();
    }

    // ---- Static openers -------------------------------------------------

    /// <summary>Mở viewer cho 1 schema diff item (table hoặc programmable).</summary>
    public static void ShowSchema(SchemaDiffItem item, DatabaseSchema source, DatabaseSchema target, Window? owner = null)
    {
        if (item == null) return;
        var win = new DiffViewerWindow { Owner = owner };
        win.LoadSchema(item, source, target);
        win.Show();
    }

    /// <summary>Mở viewer cho 1 data diff row.</summary>
    public static void ShowDataRow(DataDiffRow row, IList<string> allColumns, Window? owner = null)
    {
        if (row == null) return;
        var win = new DiffViewerWindow { Owner = owner };
        win.LoadDataRow(row, allColumns);
        win.Show();
    }

    // ---- Schema loading -------------------------------------------------

    private void LoadSchema(SchemaDiffItem item, DatabaseSchema? source, DatabaseSchema? target)
    {
        TitleText.Text = $"{item.TypeText}  {item.FullName}";
        StatusText.Text = $"Status: {item.StatusText}";
        Title = $"Diff — {item.FullName}";

        string key = item.Key;

        if (item.ObjectType == DbObjectType.Table)
        {
            LoadTable(key, source, target, item);
        }
        else
        {
            LoadProgrammable(key, source, target);
        }
    }

    private void LoadProgrammable(string key, DatabaseSchema? source, DatabaseSchema? target)
    {
        ProgrammablePanel.Visibility = Visibility.Visible;

        string srcDef = "";
        string tgtDef = "";

        if (source != null && source.Programmables.TryGetValue(key, out var sp))
            srcDef = sp.Definition ?? "";
        if (target != null && target.Programmables.TryGetValue(key, out var tp))
            tgtDef = tp.Definition ?? "";

        SourceDefBox.Text = string.IsNullOrEmpty(srcDef) ? "(not present in source)" : srcDef;
        TargetDefBox.Text = string.IsNullOrEmpty(tgtDef) ? "(not present in target)" : tgtDef;
    }

    private void LoadTable(string key, DatabaseSchema? source, DatabaseSchema? target, SchemaDiffItem item)
    {
        TablePanel.Visibility = Visibility.Visible;

        TableDef? src = null, tgt = null;
        if (source != null) source.Tables.TryGetValue(key, out src);
        if (target != null) target.Tables.TryGetValue(key, out tgt);

        SourceColsGrid.ItemsSource = ToColumnVms(src);
        TargetColsGrid.ItemsSource = ToColumnVms(tgt);

        DetailsBox.Text = item.Details.Count > 0
            ? string.Join("\r\n", item.Details)
            : "(no detailed differences)";
    }

    private static List<ColumnVm> ToColumnVms(TableDef? table)
    {
        if (table == null) return new List<ColumnVm>();
        return table.Columns
            .OrderBy(c => c.OrdinalPosition)
            .Select(c => new ColumnVm { Name = c.Name, Type = SafeType(c) })
            .ToList();
    }

    private static string SafeType(ColumnDef c)
    {
        try { return c.FullTypeText(); }
        catch { return c.DataType; }
    }

    // ---- Data row loading -----------------------------------------------

    private void LoadDataRow(DataDiffRow row, IList<string> allColumns)
    {
        Title = $"Data row — {row.Key}";
        TitleText.Text = $"Row key: {row.Key}";
        StatusText.Text = $"Status: {StatusOf(row.Status)}";

        DataRowGrid.Visibility = Visibility.Visible;

        var diffSet = new HashSet<string>(row.DifferentColumns, System.StringComparer.OrdinalIgnoreCase);

        // Tập cột: allColumns nếu có, fallback union từ 2 row.
        IEnumerable<string> columns = allColumns is { Count: > 0 }
            ? allColumns
            : UnionColumns(row);

        var vms = new List<DataCellVm>();
        foreach (var col in columns)
        {
            string sval = ReadValue(row.SourceRow, col);
            string tval = ReadValue(row.TargetRow, col);
            vms.Add(new DataCellVm
            {
                Column = col,
                SourceValue = sval,
                TargetValue = tval,
                IsDifferent = diffSet.Contains(col),
            });
        }
        DataRowGrid.ItemsSource = vms;
    }

    private static IEnumerable<string> UnionColumns(DataDiffRow row)
    {
        var cols = new List<string>();
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var src in new[] { row.SourceRow, row.TargetRow })
        {
            if (src == null) continue;
            foreach (var k in src.Values.Keys)
                if (seen.Add(k)) cols.Add(k);
        }
        return cols;
    }

    /// <summary>Đọc giá trị 1 cột từ 1 row; null/DBNull → "NULL"; row không có cột → "" (trống).</summary>
    private static string ReadValue(DataRow? row, string col)
    {
        if (row == null) return "";
        if (!row.Values.TryGetValue(col, out var val)) return "";
        if (val == null || val is System.DBNull) return "NULL";
        return val.ToString() ?? "NULL";
    }

    private static string StatusOf(DiffStatus status) => status switch
    {
        DiffStatus.OnlyInSource => "Only in Source",
        DiffStatus.OnlyInTarget => "Only in Target",
        DiffStatus.Different => "Different",
        _ => "Identical",
    };

    // ---- Small view-model records --------------------------------------

    private sealed class ColumnVm
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }

    private sealed class DataCellVm
    {
        public string Column { get; set; } = "";
        public string SourceValue { get; set; } = "";
        public string TargetValue { get; set; } = "";
        public bool IsDifferent { get; set; }
    }
}
