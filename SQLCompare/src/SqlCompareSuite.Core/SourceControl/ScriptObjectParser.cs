using System.Text.RegularExpressions;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.SourceControl;

/// <summary>
/// Parser NHẸ cho 1 file *.sql: nhận diện loại object + tên, parse cột best-effort.
/// KHÔNG phải full T-SQL parser — chỉ dựa trên regex + đếm độ sâu ngoặc.
/// Giới hạn: không xử lý được mọi cú pháp T-SQL (computed column phức tạp,
/// nhiều câu lệnh trong 1 file, table type, v.v.).
/// </summary>
internal static class ScriptObjectParser
{
    // Câu CREATE/ALTER đầu tiên + loại object + tên (có thể [schema].[name] hoặc schema.name).
    // Cho phép CREATE OR ALTER. Tên object lấy phần đầu (dừng ở khoảng trắng/'(' kế tiếp).
    private static readonly Regex CreateRegex = new(
        @"\bCREATE\s+(?:OR\s+ALTER\s+)?(TABLE|VIEW|PROCEDURE|PROC|FUNCTION|TRIGGER|SYNONYM|SEQUENCE)\s+(?<name>(?:\[[^\]]+\]|[^\s(.]+)(?:\s*\.\s*(?:\[[^\]]+\]|[^\s(.]+))?)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // Tách [schema].[name] hoặc schema.name (default schema = dbo).
    private static readonly Regex QualifiedNameRegex = new(
        @"^(?:\[(?<s>[^\]]+)\]|(?<s>[^\s.\[\]]+))\s*\.\s*(?:\[(?<n>[^\]]+)\]|(?<n>[^\s.\[\]]+))$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SingleNameRegex = new(
        @"^(?:\[(?<n>[^\]]+)\]|(?<n>[^\s.\[\]]+))$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Kết quả nhận diện 1 object từ file script.</summary>
    public sealed class ParsedObject
    {
        public string Schema { get; init; } = "dbo";
        public string Name { get; init; } = "";
        public string Keyword { get; init; } = "";  // TABLE/VIEW/PROCEDURE/...
        public string FullText { get; init; } = ""; // nguyên văn file
    }

    /// <summary>
    /// Bỏ comment đầu file, tìm câu CREATE/ALTER đầu tiên, trả về loại + tên.
    /// null nếu không nhận diện được.
    /// </summary>
    public static ParsedObject? Detect(string fileText)
    {
        if (string.IsNullOrWhiteSpace(fileText)) return null;

        string cleaned = StripLeadingComments(fileText);
        Match m = CreateRegex.Match(cleaned);
        if (!m.Success) return null;

        string keyword = m.Groups[1].Value.ToUpperInvariant();
        string rawName = m.Groups["name"].Value.Trim();
        (string schema, string name) = SplitName(rawName);
        if (string.IsNullOrWhiteSpace(name)) return null;

        return new ParsedObject
        {
            Schema = schema,
            Name = name,
            Keyword = keyword,
            FullText = fileText,
        };
    }

    /// <summary>Tách [schema].[name] thành (schema, name). Mặc định schema = dbo.</summary>
    public static (string schema, string name) SplitName(string raw)
    {
        raw = raw.Trim();
        Match q = QualifiedNameRegex.Match(raw);
        if (q.Success)
            return (Unwrap(q.Groups["s"].Value), Unwrap(q.Groups["n"].Value));

        Match s = SingleNameRegex.Match(raw);
        if (s.Success)
            return ("dbo", Unwrap(s.Groups["n"].Value));

        // Fallback: bỏ ngoặc vuông, tách theo dấu chấm.
        string clean = raw.Replace("[", "").Replace("]", "").Trim();
        int dot = clean.LastIndexOf('.');
        return dot >= 0
            ? (clean[..dot].Trim(), clean[(dot + 1)..].Trim())
            : ("dbo", clean);
    }

    private static string Unwrap(string v) => v.Trim().Trim('[', ']').Trim();

    /// <summary>
    /// Bỏ comment ở ĐẦU file (block /* */ và line --) để CreateRegex tìm đúng câu CREATE đầu tiên.
    /// Chỉ strip phần leading; comment giữa code không đụng tới.
    /// </summary>
    public static string StripLeadingComments(string text)
    {
        int i = 0;
        int n = text.Length;
        while (i < n)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }

            // Line comment --
            if (c == '-' && i + 1 < n && text[i + 1] == '-')
            {
                int eol = text.IndexOf('\n', i);
                if (eol < 0) return "";          // toàn bộ còn lại là comment
                i = eol + 1;
                continue;
            }

            // Block comment /* */
            if (c == '/' && i + 1 < n && text[i + 1] == '*')
            {
                int end = text.IndexOf("*/", i + 2, System.StringComparison.Ordinal);
                if (end < 0) return "";          // comment không đóng
                i = end + 2;
                continue;
            }

            break; // gặp ký tự code đầu tiên
        }
        return i >= n ? "" : text[i..];
    }

    // ====================== PARSE COLUMNS (CREATE TABLE) ======================

    /// <summary>
    /// Parse cột best-effort từ text CREATE TABLE. Lấy phần trong cặp ngoặc ngoài cùng
    /// sau tên bảng, tách cột ở top-level comma (đếm độ sâu ngoặc), bỏ qua dòng constraint.
    /// </summary>
    public static List<ColumnDef> ParseColumns(string fileText)
    {
        var result = new List<ColumnDef>();
        string? body = ExtractTableBody(fileText);
        if (body == null) return result;

        int ordinal = 0;
        foreach (string raw in SplitTopLevel(body))
        {
            string item = raw.Trim();
            if (item.Length == 0) continue;
            if (IsConstraintLine(item)) continue;   // bỏ PK/FK/CONSTRAINT/CHECK/UNIQUE

            ColumnDef? col = ParseColumn(item, ++ordinal);
            if (col != null) result.Add(col);
            else ordinal--; // không parse được tên → không tính ordinal
        }
        return result;
    }

    /// <summary>Lấy nội dung trong cặp ngoặc ngoài cùng đầu tiên sau "CREATE TABLE name".</summary>
    private static string? ExtractTableBody(string text)
    {
        string cleaned = StripLeadingComments(text);
        Match m = CreateRegex.Match(cleaned);
        if (!m.Success || !m.Groups[1].Value.Equals("TABLE", System.StringComparison.OrdinalIgnoreCase))
            return null;

        // Tìm '(' đầu tiên sau vị trí kết thúc match tên bảng.
        int start = m.Index + m.Length;
        int open = cleaned.IndexOf('(', start);
        if (open < 0) return null;

        int depth = 0;
        for (int i = open; i < cleaned.Length; i++)
        {
            char c = cleaned[i];
            if (c == '(') depth++;
            else if (c == ')')
            {
                depth--;
                if (depth == 0) return cleaned.Substring(open + 1, i - open - 1);
            }
        }
        return null; // ngoặc không cân bằng
    }

    /// <summary>Tách chuỗi theo dấu phẩy ở TOP-LEVEL (không nằm trong ngoặc / chuỗi).</summary>
    private static IEnumerable<string> SplitTopLevel(string body)
    {
        int depth = 0;
        int last = 0;
        bool inString = false;
        char strCh = '\'';
        for (int i = 0; i < body.Length; i++)
        {
            char c = body[i];
            if (inString)
            {
                if (c == strCh) inString = false;
                continue;
            }
            switch (c)
            {
                case '\'':
                case '"':
                    inString = true; strCh = c; break;
                case '(':
                    depth++; break;
                case ')':
                    if (depth > 0) depth--; break;
                case ',':
                    if (depth == 0)
                    {
                        yield return body[last..i];
                        last = i + 1;
                    }
                    break;
            }
        }
        if (last < body.Length) yield return body[last..];
    }

    private static readonly Regex ConstraintLineRegex = new(
        @"^\s*(?:CONSTRAINT\b|PRIMARY\s+KEY\b|FOREIGN\s+KEY\b|CHECK\b|UNIQUE\b|INDEX\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool IsConstraintLine(string item) => ConstraintLineRegex.IsMatch(item);

    // Tên cột + kiểu (kèm (...) nếu có) ở đầu định nghĩa cột.
    private static readonly Regex ColumnHeadRegex = new(
        @"^\s*(?:\[(?<col>[^\]]+)\]|(?<col>[A-Za-z_@#][\w@#$]*))\s+(?<type>\[?[A-Za-z_][\w]*\]?(?:\s*\([^)]*\))?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Tách length/precision/scale từ "(...)" của kiểu.
    private static readonly Regex TypeArgsRegex = new(
        @"\(\s*(?<a>MAX|\d+)\s*(?:,\s*(?<b>\d+)\s*)?\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex IdentityRegex = new(@"\bIDENTITY\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NotNullRegex = new(@"\bNOT\s+NULL\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NullRegex = new(@"(?<!NOT\s)\bNULL\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DefaultRegex = new(
        @"\bDEFAULT\b\s*(?<def>\((?:[^()]|\([^()]*\))*\)|'[^']*'|[^\s,]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Parse 1 định nghĩa cột → ColumnDef. null nếu không lấy được tên+kiểu.</summary>
    private static ColumnDef? ParseColumn(string item, int ordinal)
    {
        Match head = ColumnHeadRegex.Match(item);
        if (!head.Success) return null;

        string colName = Unwrap(head.Groups["col"].Value);
        string typeRaw = head.Groups["type"].Value.Trim();
        if (string.IsNullOrWhiteSpace(colName)) return null;

        var col = new ColumnDef
        {
            Name = colName,
            OrdinalPosition = ordinal,
            IsNullable = true,   // mặc định NULL nếu không khai báo NOT NULL
        };

        // Tách kiểu cơ bản + (args). VD: nvarchar(50), decimal(18,2), int.
        Match args = TypeArgsRegex.Match(typeRaw);
        string baseType = (args.Success ? typeRaw[..args.Index] : typeRaw).Trim().Trim('[', ']').Trim();
        col.DataType = baseType;

        if (args.Success)
        {
            string a = args.Groups["a"].Value;
            string b = args.Groups["b"].Value;
            string lower = baseType.ToLowerInvariant();
            bool isCharType = lower is "varchar" or "nvarchar" or "char" or "nchar" or "varbinary" or "binary";
            bool isDecimal = lower is "decimal" or "numeric";

            if (isCharType)
            {
                if (a.Equals("MAX", System.StringComparison.OrdinalIgnoreCase)) col.MaxLength = -1;
                else if (int.TryParse(a, out int len))
                {
                    // nvarchar/nchar lưu max_length theo byte (x2) để khớp SchemaReader live.
                    col.MaxLength = (lower is "nvarchar" or "nchar") ? len * 2 : len;
                }
            }
            else if (isDecimal)
            {
                if (int.TryParse(a, out int p)) col.NumericPrecision = p;
                if (!string.IsNullOrEmpty(b) && int.TryParse(b, out int s)) col.NumericScale = s;
            }
            else
            {
                // datetime2/time/datetimeoffset → scale; còn lại để nguyên DataType, giữ args trong type text.
                if (lower is "datetime2" or "time" or "datetimeoffset" && int.TryParse(a, out int sc))
                    col.NumericScale = sc;
                else
                    col.DataType = typeRaw.Trim().Trim('[', ']'); // khó parse → giữ nguyên cả "(...)"
            }
        }

        // Phần còn lại sau kiểu để dò NULL / NOT NULL / IDENTITY / DEFAULT.
        string rest = item[(head.Index + head.Length)..];

        if (IdentityRegex.IsMatch(rest)) col.IsIdentity = true;
        if (NotNullRegex.IsMatch(rest)) col.IsNullable = false;
        else if (NullRegex.IsMatch(rest)) col.IsNullable = true;

        Match def = DefaultRegex.Match(rest);
        if (def.Success) col.DefaultDefinition = def.Groups["def"].Value.Trim();

        return col;
    }
}
