using System.Text;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Reporting;

/// <summary>Xuất kết quả so sánh data của 1 table ra HTML / CSV.</summary>
public static class DataReport
{
    /// <summary>Xuất trang HTML hoàn chỉnh (CSS inline) cho kết quả so sánh data 1 table.</summary>
    public static string ToHtml(DataComparisonResult result)
    {
        result ??= new DataComparisonResult();
        var tableKey = result.TableKey ?? "";

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n");
        sb.Append("<meta charset=\"utf-8\" />\n");
        sb.Append("<title>").Append(HtmlEscape($"Data Comparison: {tableKey}")).Append("</title>\n");
        sb.Append("<style>\n");
        sb.Append("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222;}\n");
        sb.Append("h1{font-size:20px;}\n");
        sb.Append("table{border-collapse:collapse;width:100%;margin:12px 0;}\n");
        sb.Append("th,td{border:1px solid #ccc;padding:6px 10px;text-align:left;font-size:13px;vertical-align:top;}\n");
        sb.Append("th{background:#f0f0f0;}\n");
        sb.Append(".only-source{background:#fde8e8;}\n");
        sb.Append(".only-target{background:#e8f0fe;}\n");
        sb.Append(".different{background:#fff4d6;}\n");
        sb.Append(".identical{background:#eafbe8;}\n");
        sb.Append(".summary td,.summary th{text-align:center;}\n");
        sb.Append("</style>\n</head>\n<body>\n");

        sb.Append("<h1>").Append(HtmlEscape($"Data Comparison: {tableKey}")).Append("</h1>\n");

        // Bảng summary counts
        sb.Append("<h2>Summary</h2>\n");
        sb.Append("<table class=\"summary\">\n<tr>");
        sb.Append("<th>Only in Source</th><th>Only in Target</th><th>Different</th><th>Identical</th><th>Total Rows</th>");
        sb.Append("</tr>\n<tr>");
        int total = result.Rows?.Count ?? 0;
        sb.Append("<td class=\"only-source\">").Append(result.CountOnlyInSource).Append("</td>");
        sb.Append("<td class=\"only-target\">").Append(result.CountOnlyInTarget).Append("</td>");
        sb.Append("<td class=\"different\">").Append(result.CountDifferent).Append("</td>");
        sb.Append("<td class=\"identical\">").Append(result.CountIdentical).Append("</td>");
        sb.Append("<td>").Append(total).Append("</td>");
        sb.Append("</tr>\n</table>\n");

        // Bảng chi tiết các row diff
        sb.Append("<h2>Rows</h2>\n");
        sb.Append("<table>\n<tr><th>Key</th><th>Status</th><th>Different Columns</th></tr>\n");
        if (result.Rows != null)
        {
            foreach (var row in result.Rows)
            {
                if (row == null) continue;
                var cls = StatusCssClass(row.Status);
                var diffCols = row.DifferentColumns != null ? string.Join(", ", row.DifferentColumns) : "";
                sb.Append("<tr class=\"").Append(cls).Append("\">");
                sb.Append("<td>").Append(HtmlEscape(row.Key)).Append("</td>");
                sb.Append("<td>").Append(HtmlEscape(StatusText(row.Status))).Append("</td>");
                sb.Append("<td>").Append(HtmlEscape(diffCols)).Append("</td>");
                sb.Append("</tr>\n");
            }
        }
        sb.Append("</table>\n</body>\n</html>");
        return sb.ToString();
    }

    /// <summary>Xuất kết quả so sánh data ra CSV (header + escape chuẩn).</summary>
    public static string ToCsv(DataComparisonResult result)
    {
        result ??= new DataComparisonResult();

        var sb = new StringBuilder();
        sb.Append("Key,Status,DifferentColumns\r\n");
        if (result.Rows != null)
        {
            foreach (var row in result.Rows)
            {
                if (row == null) continue;
                var diffCols = row.DifferentColumns != null ? string.Join(", ", row.DifferentColumns) : "";
                sb.Append(CsvEscape(row.Key)).Append(',');
                sb.Append(CsvEscape(StatusText(row.Status))).Append(',');
                sb.Append(CsvEscape(diffCols)).Append("\r\n");
            }
        }
        return sb.ToString();
    }

    // Text trạng thái diff (DataDiffRow không có sẵn StatusText).
    private static string StatusText(DiffStatus status) => status switch
    {
        DiffStatus.OnlyInSource => "Only in Source",
        DiffStatus.OnlyInTarget => "Only in Target",
        DiffStatus.Different => "Different",
        _ => "Identical",
    };

    // CSS class theo trạng thái diff.
    private static string StatusCssClass(DiffStatus status) => status switch
    {
        DiffStatus.OnlyInSource => "only-source",
        DiffStatus.OnlyInTarget => "only-target",
        DiffStatus.Different => "different",
        _ => "identical",
    };

    // Escape ký tự đặc biệt HTML.
    private static string HtmlEscape(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
    }

    // Escape 1 field CSV: bọc nháy kép, double-quote dấu nháy bên trong.
    private static string CsvEscape(string? s)
    {
        s ??= "";
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
