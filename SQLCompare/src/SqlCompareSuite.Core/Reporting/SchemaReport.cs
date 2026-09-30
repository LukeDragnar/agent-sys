using System.Text;
using System.Xml.Linq;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Reporting;

/// <summary>Xuất kết quả so sánh schema ra HTML / XML / CSV.</summary>
public static class SchemaReport
{
    /// <summary>Xuất trang HTML hoàn chỉnh (CSS inline) cho kết quả so sánh schema.</summary>
    public static string ToHtml(SchemaComparisonResult result, string sourceName, string targetName)
    {
        // null-safe: tránh NRE nếu caller truyền null
        result ??= new SchemaComparisonResult();
        sourceName ??= "";
        targetName ??= "";

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n");
        sb.Append("<meta charset=\"utf-8\" />\n");
        sb.Append("<title>").Append(HtmlEscape($"Schema Comparison: {sourceName} vs {targetName}")).Append("</title>\n");
        sb.Append("<style>\n");
        sb.Append("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222;}\n");
        sb.Append("h1{font-size:20px;}\n");
        sb.Append("table{border-collapse:collapse;width:100%;margin:12px 0;}\n");
        sb.Append("th,td{border:1px solid #ccc;padding:6px 10px;text-align:left;font-size:13px;vertical-align:top;}\n");
        sb.Append("th{background:#f0f0f0;}\n");
        sb.Append(".only-source{background:#fde8e8;}\n");   // đỏ nhạt
        sb.Append(".only-target{background:#e8f0fe;}\n");   // xanh nhạt
        sb.Append(".different{background:#fff4d6;}\n");     // vàng nhạt
        sb.Append(".identical{background:#eafbe8;}\n");     // xanh lá nhạt
        sb.Append(".summary td,.summary th{text-align:center;}\n");
        sb.Append("</style>\n</head>\n<body>\n");

        sb.Append("<h1>").Append(HtmlEscape($"Schema Comparison: {sourceName} vs {targetName}")).Append("</h1>\n");

        // Bảng summary các counts
        sb.Append("<h2>Summary</h2>\n");
        sb.Append("<table class=\"summary\">\n<tr>");
        sb.Append("<th>Only in Source</th><th>Only in Target</th><th>Different</th><th>Identical</th><th>Total</th>");
        sb.Append("</tr>\n<tr>");
        int total = result.Items?.Count ?? 0;
        sb.Append("<td class=\"only-source\">").Append(result.CountOnlyInSource).Append("</td>");
        sb.Append("<td class=\"only-target\">").Append(result.CountOnlyInTarget).Append("</td>");
        sb.Append("<td class=\"different\">").Append(result.CountDifferent).Append("</td>");
        sb.Append("<td class=\"identical\">").Append(result.CountIdentical).Append("</td>");
        sb.Append("<td>").Append(total).Append("</td>");
        sb.Append("</tr>\n</table>\n");

        // Bảng chi tiết từng object
        sb.Append("<h2>Objects</h2>\n");
        sb.Append("<table>\n<tr><th>Type</th><th>Object</th><th>Status</th><th>Details</th></tr>\n");
        if (result.Items != null)
        {
            foreach (var item in result.Items)
            {
                if (item == null) continue;
                var cls = StatusCssClass(item.Status);
                var details = item.Details != null ? string.Join("; ", item.Details) : "";
                sb.Append("<tr class=\"").Append(cls).Append("\">");
                sb.Append("<td>").Append(HtmlEscape(item.TypeText)).Append("</td>");
                sb.Append("<td>").Append(HtmlEscape(item.FullName)).Append("</td>");
                sb.Append("<td>").Append(HtmlEscape(item.StatusText)).Append("</td>");
                sb.Append("<td>").Append(HtmlEscape(details)).Append("</td>");
                sb.Append("</tr>\n");
            }
        }
        sb.Append("</table>\n</body>\n</html>");
        return sb.ToString();
    }

    /// <summary>Xuất kết quả so sánh schema ra XML (System.Xml.Linq).</summary>
    public static string ToXml(SchemaComparisonResult result)
    {
        result ??= new SchemaComparisonResult();

        var summary = new XElement("Summary",
            new XAttribute("onlyInSource", result.CountOnlyInSource),
            new XAttribute("onlyInTarget", result.CountOnlyInTarget),
            new XAttribute("different", result.CountDifferent),
            new XAttribute("identical", result.CountIdentical),
            new XAttribute("total", result.Items?.Count ?? 0));

        var objects = new XElement("Objects");
        if (result.Items != null)
        {
            foreach (var item in result.Items)
            {
                if (item == null) continue;
                var objEl = new XElement("Object",
                    new XAttribute("type", item.TypeText),
                    new XAttribute("status", item.StatusText),
                    new XAttribute("name", item.FullName));
                if (item.Details != null)
                {
                    foreach (var d in item.Details)
                        objEl.Add(new XElement("Detail", d ?? ""));
                }
                objects.Add(objEl);
            }
        }

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("SchemaComparison", summary, objects));
        return doc.Declaration + "\n" + doc.ToString();
    }

    /// <summary>Xuất kết quả so sánh schema ra CSV (header + escape chuẩn).</summary>
    public static string ToCsv(SchemaComparisonResult result)
    {
        result ??= new SchemaComparisonResult();

        var sb = new StringBuilder();
        sb.Append("Type,Object,Status,Details\r\n");
        if (result.Items != null)
        {
            foreach (var item in result.Items)
            {
                if (item == null) continue;
                var details = item.Details != null ? string.Join("; ", item.Details) : "";
                sb.Append(CsvEscape(item.TypeText)).Append(',');
                sb.Append(CsvEscape(item.FullName)).Append(',');
                sb.Append(CsvEscape(item.StatusText)).Append(',');
                sb.Append(CsvEscape(details)).Append("\r\n");
            }
        }
        return sb.ToString();
    }

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
