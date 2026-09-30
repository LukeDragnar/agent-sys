using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Data;

/// <summary>
/// Đọc metadata (PK columns, identity column, danh sách table có PK, danh sách cột)
/// và đọc data rows từ 1 table. Tách riêng khỏi facade để tái sử dụng.
/// </summary>
internal static class DataTableReader
{
    /// <summary>Tách "schema.table" -> (schema, table). Mặc định schema = dbo nếu thiếu.</summary>
    public static (string Schema, string Table) SplitSchemaTable(string schemaTable)
    {
        if (string.IsNullOrWhiteSpace(schemaTable))
            throw new ArgumentException("schemaTable rỗng.", nameof(schemaTable));

        var idx = schemaTable.IndexOf('.');
        if (idx < 0)
            return ("dbo", schemaTable.Trim());

        var schema = schemaTable[..idx].Trim();
        var table = schemaTable[(idx + 1)..].Trim();
        if (schema.Length == 0) schema = "dbo";
        return (schema, table);
    }

    /// <summary>Danh sách "schema.table" có primary key, sắp xếp theo tên.</summary>
    public static List<string> GetTablesWithPrimaryKey(DbConnectionInfo conn)
    {
        const string sql = @"
SELECT DISTINCT s.name AS SchemaName, t.name AS TableName
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.indexes i ON i.object_id = t.object_id AND i.is_primary_key = 1
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;";

        var result = new List<string>();

        using var connection = new SqlConnection(conn.BuildConnectionString());
        connection.OpenRead();
        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            result.Add($"{schema}.{table}");
        }
        return result;
    }

    /// <summary>
    /// Danh sách TẤT CẢ user table "schema.table" (bỏ table hệ thống), sắp xếp theo tên.
    /// Dùng được vì Compare đã có fallback key (unique index / all-columns) khi không có PK.
    /// </summary>
    public static List<string> GetAllUserTables(DbConnectionInfo conn)
    {
        const string sql = @"
SELECT s.name AS SchemaName, t.name AS TableName
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;";

        var result = new List<string>();

        using var connection = new SqlConnection(conn.BuildConnectionString());
        connection.OpenRead();
        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            result.Add($"{schema}.{table}");
        }
        return result;
    }

    /// <summary>Đọc cột PK theo đúng thứ tự key. Rỗng nếu table không có PK.</summary>
    public static List<string> GetPrimaryKeyColumns(SqlConnection connection, string schema, string table)
    {
        const string sql = @"
SELECT c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.tables t       ON t.object_id = i.object_id
INNER JOIN sys.schemas s      ON s.schema_id = t.schema_id
INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c      ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.is_primary_key = 1
  AND s.name = @schema
  AND t.name = @table
ORDER BY ic.key_ordinal;";

        var cols = new List<string>();
        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            cols.Add(reader.GetString(0));
        return cols;
    }

    /// <summary>
    /// Cột của 1 unique index BẤT KỲ (ưu tiên index không nullable, ít cột nhất) để fallback làm key.
    /// Rỗng nếu table không có unique index nào dùng được.
    /// </summary>
    public static List<string> GetUniqueIndexColumns(SqlConnection connection, string schema, string table)
    {
        // Chọn 1 unique index (không phải PK) tốt nhất:
        // ưu tiên index KHÔNG có cột nullable, rồi tới ít cột nhất, rồi index_id nhỏ nhất.
        const string sql = @"
SELECT TOP 1 i.index_id
FROM sys.indexes i
INNER JOIN sys.tables t  ON t.object_id = i.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.is_unique = 1
  AND i.is_primary_key = 0
  AND i.is_disabled = 0
  AND i.type IN (1, 2) -- clustered / nonclustered
  AND s.name = @schema
  AND t.name = @table
  AND ic.is_included_column = 0
GROUP BY i.index_id
ORDER BY
  MAX(CASE WHEN c.is_nullable = 1 THEN 1 ELSE 0 END) ASC,
  COUNT(*) ASC,
  i.index_id ASC;";

        int? indexId = null;
        using (var cmd = new SqlCommand(sql, connection))
        {
            cmd.Parameters.AddWithValue("@schema", schema);
            cmd.Parameters.AddWithValue("@table", table);
            var obj = cmd.ExecuteScalar();
            if (obj is int id) indexId = id;
            else if (obj is not null && obj is not DBNull) indexId = Convert.ToInt32(obj, CultureInfo.InvariantCulture);
        }

        if (indexId is null) return new List<string>();

        // Lấy cột của index đã chọn theo đúng thứ tự key_ordinal.
        const string colSql = @"
SELECT c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.tables t  ON t.object_id = i.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE s.name = @schema
  AND t.name = @table
  AND i.index_id = @indexId
  AND ic.is_included_column = 0
ORDER BY ic.key_ordinal;";

        var cols = new List<string>();
        using var colCmd = new SqlCommand(colSql, connection);
        colCmd.Parameters.AddWithValue("@schema", schema);
        colCmd.Parameters.AddWithValue("@table", table);
        colCmd.Parameters.AddWithValue("@indexId", indexId.Value);
        using var reader = colCmd.ExecuteReader();
        while (reader.Read())
            cols.Add(reader.GetString(0));
        return cols;
    }

    /// <summary>
    /// Validate các cột tồn tại trong table (so theo tên, không phân biệt hoa thường).
    /// Ném InvalidOperationException liệt kê các cột không tồn tại nếu có.
    /// Dùng cho CustomKeyColumns.
    /// </summary>
    public static void ValidateColumnsExist(
        SqlConnection connection, string schema, string table, List<string> columns)
    {
        if (columns is null || columns.Count == 0) return;

        var actual = GetAllColumns(connection, schema, table);
        var actualSet = new HashSet<string>(actual, StringComparer.OrdinalIgnoreCase);

        // Liệt kê các cột không tồn tại để báo lỗi rõ ràng
        var missing = columns.Where(c => !actualSet.Contains(c)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"CustomKeyColumns không hợp lệ cho '{schema}.{table}': " +
                $"không tồn tại cột [{string.Join(", ", missing)}]. " +
                $"Cột hiện có: [{string.Join(", ", actual)}].");
    }

    /// <summary>Tất cả cột (theo column_id) của table — dùng làm key fallback cuối cùng.</summary>
    public static List<string> GetAllColumns(SqlConnection connection, string schema, string table)
    {
        const string sql = @"
SELECT c.name AS ColumnName
FROM sys.columns c
INNER JOIN sys.tables t  ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @schema
  AND t.name = @table
ORDER BY c.column_id;";

        var cols = new List<string>();
        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            cols.Add(reader.GetString(0));
        return cols;
    }

    /// <summary>Tên cột identity của table (null nếu không có).</summary>
    public static string? GetIdentityColumn(SqlConnection connection, string schema, string table)
    {
        const string sql = @"
SELECT c.name AS ColumnName
FROM sys.columns c
INNER JOIN sys.tables t  ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE c.is_identity = 1
  AND s.name = @schema
  AND t.name = @table;";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);
        var obj = cmd.ExecuteScalar();
        return obj is string name ? name : null;
    }

    /// <summary>
    /// Đọc toàn bộ rows của table, ORDER BY theo PK. Trả ra (allColumns theo thứ tự, danh sách DataRow).
    /// Key của mỗi row ghép từ giá trị PK bằng '|' (null -> &lt;NULL&gt;, InvariantCulture).
    /// </summary>
    public static (List<string> AllColumns, List<DataRow> Rows) ReadRows(
        SqlConnection connection, string schema, string table, List<string> pkColumns,
        string? whereClause = null)
    {
        var tableRef = SqlLiteral.BracketTable(schema, table);
        var orderBy = string.Join(", ", pkColumns.Select(SqlLiteral.Bracket));
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var sql = $"SELECT * FROM {tableRef}{where} ORDER BY {orderBy};";

        var allColumns = new List<string>();
        var rows = new List<DataRow>();

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        // Lấy danh sách cột theo thứ tự ordinal
        for (var i = 0; i < reader.FieldCount; i++)
            allColumns.Add(reader.GetName(i));

        // Index của các cột PK trong reader để build key nhanh
        var pkOrdinals = pkColumns
            .Select(pk => allColumns.FindIndex(c => string.Equals(c, pk, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        while (reader.Read())
        {
            var row = new DataRow();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                // DBNull -> null để so sánh/format thống nhất
                var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row.Values[allColumns[i]] = val;
            }

            row.Key = BuildKey(reader, pkOrdinals);
            rows.Add(row);
        }

        return (allColumns, rows);
    }

    /// <summary>
    /// Ghép key từ các cột PK (theo ordinal trong reader hiện tại).
    /// TYPE-AWARE + LENGTH-PREFIX để 2 tổ hợp giá trị khác nhau KHÔNG BAO GIỜ ra cùng key:
    /// - null/DBNull -> token cố định.
    /// - byte[] -> hex (rowversion/binary PK không bị collapse "System.Byte[]").
    /// - DateTime/DateTimeOffset -> round-trip "o" (giữ sub-second precision).
    /// - float/double -> "R"; decimal -> InvariantCulture (tránh mất precision).
    /// - bool -> "0"/"1".
    /// Mỗi phần được length-prefix (len ':' value) nên separator trong value không gây collision.
    /// </summary>
    private static string BuildKey(SqlDataReader reader, List<int> pkOrdinals)
    {
        var sb = new StringBuilder();
        for (var k = 0; k < pkOrdinals.Count; k++)
        {
            var ord = pkOrdinals[k];
            object? val = (ord < 0 || reader.IsDBNull(ord)) ? null : reader.GetValue(ord);
            var part = KeyPart(val);
            // Length-prefix: ghi độ dài + ':' + nội dung -> không thể nhập nhằng ranh giới phần.
            sb.Append(part.Length).Append(':').Append(part).Append('|');
        }
        return sb.ToString();
    }

    /// <summary>
    /// Chuẩn hóa 1 giá trị key thành chuỗi ĐỊNH DANH ổn định, type-aware.
    /// Dùng CHUNG cho mọi nơi build key (source và target) để bảo đảm cùng chuẩn.
    /// </summary>
    private static string KeyPart(object? val)
    {
        switch (val)
        {
            case null:
            case DBNull:
                return "\0NULL\0";
            case byte[] bytes:
                // hex: mỗi byte 2 ký tự, không bị "System.Byte[]" cho mọi row.
                return BitConverter.ToString(bytes);
            case bool b:
                return b ? "1" : "0";
            case DateTime dt:
                return dt.ToString("o", CultureInfo.InvariantCulture);
            case DateTimeOffset dto:
                return dto.ToString("o", CultureInfo.InvariantCulture);
            case float f:
                return f.ToString("R", CultureInfo.InvariantCulture);
            case double d:
                return d.ToString("R", CultureInfo.InvariantCulture);
            case decimal dec:
                return dec.ToString(CultureInfo.InvariantCulture);
            default:
                return Convert.ToString(val, CultureInfo.InvariantCulture) ?? "\0NULL\0";
        }
    }

    /// <summary>
    /// Stream rows của 1 table theo ORDER BY key, đọc TỪNG DÒNG (không nạp hết vào RAM).
    /// Dùng cho merge-join bảng lớn. Nhớ Dispose để đóng reader/command/connection-state.
    /// </summary>
    public sealed class OrderedRowStream : IDisposable
    {
        private readonly SqlCommand _cmd;
        private readonly SqlDataReader _reader;
        private readonly List<int> _pkOrdinals;

        public List<string> AllColumns { get; } = new();

        /// <summary>Row hiện tại (null nếu đã hết / chưa đọc lần nào).</summary>
        public DataRow? Current { get; private set; }

        /// <summary>True khi đã đọc hết stream.</summary>
        public bool Eof { get; private set; }

        public OrderedRowStream(SqlConnection connection, string schema, string table,
            List<string> keyColumns, string? whereClause, int? topN = null)
        {
            var tableRef = SqlLiteral.BracketTable(schema, table);
            var orderBy = string.Join(", ", keyColumns.Select(SqlLiteral.Bracket));
            var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
            // TOP (N): chỉ áp khi topN > 0; N nhúng trực tiếp (đã ép int an toàn).
            var top = topN is > 0 ? $"TOP ({topN.Value}) " : "";
            var sql = $"SELECT {top}* FROM {tableRef}{where} ORDER BY {orderBy};";

            _cmd = new SqlCommand(sql, connection);
            _reader = _cmd.ExecuteReader();

            for (var i = 0; i < _reader.FieldCount; i++)
                AllColumns.Add(_reader.GetName(i));

            _pkOrdinals = keyColumns
                .Select(pk => AllColumns.FindIndex(c => string.Equals(c, pk, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        /// <summary>Đọc dòng kế tiếp vào Current. Trả false khi hết.</summary>
        public bool MoveNext()
        {
            if (Eof) { Current = null; return false; }
            if (!_reader.Read())
            {
                Eof = true;
                Current = null;
                return false;
            }

            var row = new DataRow();
            for (var i = 0; i < _reader.FieldCount; i++)
            {
                var val = _reader.IsDBNull(i) ? null : _reader.GetValue(i);
                row.Values[AllColumns[i]] = val;
            }
            row.Key = BuildKey(_reader, _pkOrdinals);
            Current = row;
            return true;
        }

        public void Dispose()
        {
            _reader.Dispose();
            _cmd.Dispose();
        }
    }
}
