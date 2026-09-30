using System.Globalization;
using System.Text;

namespace SqlCompareSuite.Core.Data;

/// <summary>Format giá trị .NET thành SQL literal an toàn (escape nháy đơn, đúng kiểu).</summary>
internal static class SqlLiteral
{
    /// <summary>Bọc tên schema/table/column bằng [] và escape ] thành ]].</summary>
    public static string Bracket(string name) => "[" + name.Replace("]", "]]") + "]";

    /// <summary>Ghép "[schema].[table]" từ chuỗi "schema.table".</summary>
    public static string BracketTable(string schema, string table) => $"{Bracket(schema)}.{Bracket(table)}";

    /// <summary>
    /// Chuyển 1 giá trị thành literal SQL.
    /// null/DBNull -> NULL; string -> N'...'; bool -> 0/1; số giữ nguyên;
    /// datetime -> ISO; guid -> '...'; byte[] -> 0x...; còn lại -> N'ToString()'.
    /// </summary>
    public static string Format(object? value)
    {
        if (value is null || value is DBNull)
            return "NULL";

        switch (value)
        {
            case string s:
                return "N'" + EscapeQuotes(s) + "'";

            case bool b:
                return b ? "1" : "0";

            case byte or sbyte or short or ushort or int or uint or long or ulong:
                return Convert.ToString(value, CultureInfo.InvariantCulture)!;

            case decimal dec:
                return dec.ToString(CultureInfo.InvariantCulture);

            case float f:
                // R = round-trip, tránh mất precision
                return f.ToString("R", CultureInfo.InvariantCulture);

            case double d:
                return d.ToString("R", CultureInfo.InvariantCulture);

            case DateTime dt:
                // ISO8601 round-trip 7 chữ số -> giữ đủ precision datetime2(7).
                // (datetime cũ chỉ có .000... vẫn parse đúng; SQL Server đọc được ISO 7 chữ số.)
                return "'" + dt.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "'";

            case DateTimeOffset dto:
                // Giữ offset + 7 chữ số sub-second cho datetimeoffset(7).
                return "'" + dto.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture) + "'";

            case TimeSpan ts:
                return "'" + ts.ToString() + "'";

            case Guid g:
                return "'" + g.ToString() + "'";

            case byte[] bytes:
                return FormatBinary(bytes);

            default:
                // Fallback: coi như chuỗi, escape an toàn
                return "N'" + EscapeQuotes(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "") + "'";
        }
    }

    /// <summary>Escape dấu nháy đơn cho literal chuỗi.</summary>
    private static string EscapeQuotes(string s) => s.Replace("'", "''");

    /// <summary>byte[] -> 0xABCDEF (rỗng -> 0x).</summary>
    private static string FormatBinary(byte[] bytes)
    {
        var sb = new StringBuilder(2 + bytes.Length * 2);
        sb.Append("0x");
        foreach (var bt in bytes)
            sb.Append(bt.ToString("X2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }
}
