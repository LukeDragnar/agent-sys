using System.Text;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Schema;

/// <summary>Tùy chọn sinh sync script.</summary>
public sealed class SchemaScriptOptions
{
    /// <summary>Bọc toàn bộ script trong BEGIN/COMMIT TRANSACTION.</summary>
    public bool WrapInTransaction { get; set; } = true;

    /// <summary>Có sinh các câu DROP (object thừa ở target, drop column, drop constraint) hay không.</summary>
    public bool IncludeDrops { get; set; } = true;
}

/// <summary>Kết quả sinh sync script kèm danh sách cảnh báo (data loss, drop, dependency break).</summary>
public sealed class SyncScriptResult
{
    public string Script = "";
    public List<string> Warnings = new();
}

/// <summary>
/// Sinh script SQL để TARGET trở nên giống SOURCE, chỉ cho các item được chọn.
/// Sinh theo đúng thứ tự phụ thuộc: drop FK -> drop objects -> create tables (topo) ->
/// alter tables -> programmables -> add FK.
/// </summary>
public sealed class SchemaScriptGenerator
{
    /// <summary>Overload cũ (giữ tương thích): dùng options mặc định.</summary>
    public string GenerateSyncScript(DatabaseSchema source, DatabaseSchema target, IEnumerable<SchemaDiffItem> selected)
        => GenerateSyncScript(source, target, selected, new SchemaScriptOptions());

    /// <summary>Overload (giữ tương thích): trả về string. Gọi method có warnings rồi lấy .Script.</summary>
    public string GenerateSyncScript(DatabaseSchema source, DatabaseSchema target, IEnumerable<SchemaDiffItem> selected, SchemaScriptOptions options)
        => GenerateSyncScriptWithWarnings(source, target, selected, options).Script;

    /// <summary>
    /// Sinh sync script + thu thập cảnh báo (data loss, object drop, column drop, dependency break).
    /// </summary>
    public SyncScriptResult GenerateSyncScriptWithWarnings(
        DatabaseSchema source, DatabaseSchema target, IEnumerable<SchemaDiffItem> selected, SchemaScriptOptions options)
    {
        options ??= new SchemaScriptOptions();
        var sb = new StringBuilder();
        var warnings = new List<string>();

        var items = selected.ToList();

        // Phân loại item theo nhóm để sinh đúng thứ tự
        var tableOnlyInSource = items.Where(i => i.ObjectType == DbObjectType.Table && i.Status == DiffStatus.OnlyInSource).ToList();
        var tableOnlyInTarget = items.Where(i => i.ObjectType == DbObjectType.Table && i.Status == DiffStatus.OnlyInTarget).ToList();
        var tableDifferent = items.Where(i => i.ObjectType == DbObjectType.Table && i.Status == DiffStatus.Different).ToList();
        var sequences = items.Where(i => i.ObjectType == DbObjectType.Sequence).ToList();
        var synonyms = items.Where(i => i.ObjectType == DbObjectType.Synonym).ToList();
        var schemas = items.Where(i => i.ObjectType == DbObjectType.Schema).ToList();
        // SECURITY
        var roles = items.Where(i => i.ObjectType == DbObjectType.Role).ToList();
        var users = items.Where(i => i.ObjectType == DbObjectType.User).ToList();
        var permissions = items.Where(i => i.ObjectType == DbObjectType.Permission).ToList();
        // PARTITION
        var partitionFunctions = items.Where(i => i.ObjectType == DbObjectType.PartitionFunction).ToList();
        var partitionSchemes = items.Where(i => i.ObjectType == DbObjectType.PartitionScheme).ToList();
        var programmables = items.Where(i =>
            i.ObjectType == DbObjectType.View || i.ObjectType == DbObjectType.StoredProcedure ||
            i.ObjectType == DbObjectType.Function || i.ObjectType == DbObjectType.Trigger).ToList();

        // Khi WrapInTransaction = true: KHÔNG dùng GO (GO phá transaction),
        // programmable wrap bằng EXEC(N'...') để gói trong 1 batch.
        var inTx = options.WrapInTransaction;

        if (inTx)
        {
            sb.AppendLine("BEGIN TRANSACTION;");
            sb.AppendLine();
        }

        // ---- 0. CREATE SCHEMA RẤT SỚM (trước mọi object dùng schema đó) + DROP SCHEMA thừa (guard) ----
        AppendSchemas(sb, schemas, options, inTx, warnings);

        // ---- 0a. SECURITY: ROLES + USERS sớm (trước khi gán membership / cấp quyền) ----
        AppendRoles(sb, roles, options, inTx, warnings);
        AppendUsers(sb, source, target, users, options, inTx);
        AppendRoleMemberships(sb, source, users, inTx);

        // ---- 0b. PARTITION: function trước, scheme sau — TRƯỚC CREATE TABLE (table tham chiếu scheme) ----
        AppendPartitionFunctions(sb, source, target, partitionFunctions, options, inTx);
        AppendPartitionSchemes(sb, source, target, partitionSchemes, options, inTx);

        // ---- 1. DROP các foreign key cần xóa TRƯỚC (tránh lỗi ràng buộc) ----
        // Gồm: FK của bảng bị DROP (OnlyInTarget) + FK thừa/khác ở bảng Different.
        AppendDropForeignKeys(sb, source, target, tableOnlyInTarget, tableDifferent, options, inTx);

        // ---- 2. DROP objects thừa ở target (OnlyInTarget) — table + programmable ----
        if (options.IncludeDrops)
        {
            AppendDropTables(sb, target, tableOnlyInTarget, inTx);
            AppendDropProgrammables(sb, target, programmables, inTx);
        }

        // ---- 2b. SEQUENCES: tạo/sửa/xóa TRƯỚC khi tạo bảng (cột default có thể tham chiếu sequence) ----
        AppendSequences(sb, source, target, sequences, options, inTx);

        // ---- 2c. SYNONYMS: tạo/sửa/xóa ----
        AppendSynonyms(sb, source, target, synonyms, options, inTx);

        // ---- 3. CREATE tables mới (OnlyInSource) theo thứ tự topological FK ----
        AppendCreateTablesTopological(sb, source, tableOnlyInSource, inTx);

        // ---- 4. ALTER tables (Different): ADD/ALTER/DROP COLUMN + check constraints + index ----
        AppendAlterTables(sb, source, target, tableDifferent, options, inTx);

        // ---- 5. CREATE OR ALTER programmables theo nhóm: View -> Function -> SP -> Trigger ----
        AppendProgrammables(sb, source, target, programmables, inTx);

        // ---- 6. ADD tất cả FOREIGN KEY (bảng mới + FK thiếu/khác ở bảng Different) — SAU CÙNG ----
        AppendAddForeignKeys(sb, source, target, tableOnlyInSource, tableDifferent, inTx);

        // ---- 7. PERMISSIONS GẦN CUỐI (sau khi object tồn tại để GRANT/REVOKE ON object hợp lệ) ----
        AppendPermissions(sb, source, target, permissions, options, inTx);

        if (inTx)
        {
            sb.AppendLine("COMMIT TRANSACTION;");
            sb.AppendLine();
        }

        // ---- Thu thập warnings (phân tích trên diff, độc lập với script text) ----
        CollectWarnings(source, target, tableOnlyInTarget, tableDifferent, options, warnings);

        return new SyncScriptResult { Script = sb.ToString(), Warnings = warnings };
    }

    // ====================== 1. DROP FOREIGN KEYS ======================

    /// <summary>Drop FK của bảng sắp bị xóa + FK thừa/khác ở bảng Different.</summary>
    private static void AppendDropForeignKeys(
        StringBuilder sb, DatabaseSchema source, DatabaseSchema target,
        List<SchemaDiffItem> tableOnlyInTarget, List<SchemaDiffItem> tableDifferent,
        SchemaScriptOptions options, bool inTx)
    {
        var emittedHeader = false;

        // FK của bảng sắp bị DROP — chỉ drop nếu IncludeDrops (drop table sẽ kéo theo, nhưng drop FK trước cho an toàn)
        if (options.IncludeDrops)
        {
            foreach (var item in tableOnlyInTarget)
            {
                if (!target.Tables.TryGetValue(item.Key, out var tgt)) continue;
                foreach (var fk in tgt.ForeignKeys)
                {
                    EnsureHeader(sb, ref emittedHeader, "DROP FOREIGN KEYS (bước 1)");
                    AppendDropFkStmt(sb, tgt, fk);
                }
            }
        }

        // FK ở bảng Different: drop FK thừa (chỉ ở target) hoặc FK khác (signature đổi → drop để add lại sau)
        foreach (var item in tableDifferent)
        {
            if (!source.Tables.TryGetValue(item.Key, out var src)) continue;
            if (!target.Tables.TryGetValue(item.Key, out var tgt)) continue;

            var srcFk = src.ForeignKeys.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var fk in tgt.ForeignKeys)
            {
                var exists = srcFk.TryGetValue(fk.Name, out var sfk);
                // Drop nếu: FK thừa ở target (IncludeDrops), hoặc FK đổi signature (cần drop để add lại)
                var changed = exists && sfk!.Signature() != fk.Signature();
                var removed = !exists;

                if (changed || (removed && options.IncludeDrops))
                {
                    EnsureHeader(sb, ref emittedHeader, "DROP FOREIGN KEYS (bước 1)");
                    AppendDropFkStmt(sb, tgt, fk);
                }
            }
        }

        if (emittedHeader) AppendSeparator(sb, inTx);
    }

    /// <summary>Câu DROP FK có guard IF EXISTS.</summary>
    private static void AppendDropFkStmt(StringBuilder sb, TableDef table, ForeignKeyDef fk)
    {
        // Guard: chỉ drop nếu FK tồn tại
        sb.AppendLine($"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{Q(fk.Name)}' AND parent_object_id = OBJECT_ID(N'{Q(table.FullName)}'))");
        sb.AppendLine($"    ALTER TABLE {table.FullName} DROP CONSTRAINT {B(fk.Name)};");
    }

    // ====================== 2. DROP OBJECTS ======================

    /// <summary>Drop các table thừa ở target — guard IF OBJECT_ID, cảnh báo mất data.</summary>
    private static void AppendDropTables(StringBuilder sb, DatabaseSchema target, List<SchemaDiffItem> tableOnlyInTarget, bool inTx)
    {
        if (tableOnlyInTarget.Count == 0) return;

        AppendHeader(sb, "DROP TABLES thừa ở target (bước 2)");
        foreach (var item in tableOnlyInTarget)
        {
            if (!target.Tables.TryGetValue(item.Key, out var tgt)) continue;
            sb.AppendLine($"-- CẢNH BÁO: DROP TABLE sẽ mất toàn bộ dữ liệu của {tgt.FullName}");
            sb.AppendLine($"IF OBJECT_ID(N'{Q(tgt.FullName)}', N'U') IS NOT NULL");
            sb.AppendLine($"    DROP TABLE {tgt.FullName};");
        }
        AppendSeparator(sb, inTx);
    }

    /// <summary>Drop các programmable thừa ở target (OnlyInTarget) — guard IF EXISTS.</summary>
    private static void AppendDropProgrammables(StringBuilder sb, DatabaseSchema target, List<SchemaDiffItem> programmables, bool inTx)
    {
        var drops = programmables.Where(i => i.Status == DiffStatus.OnlyInTarget).ToList();
        if (drops.Count == 0) return;

        AppendHeader(sb, "DROP PROGRAMMABLES thừa ở target (bước 2)");
        foreach (var item in drops)
        {
            if (!target.Programmables.TryGetValue(item.Key, out var tgt)) continue;
            sb.AppendLine($"DROP {DropKeyword(tgt.ObjectType)} IF EXISTS {tgt.FullName};");
        }
        AppendSeparator(sb, inTx);
    }

    // ====================== 2b. SEQUENCES ======================

    /// <summary>
    /// Sinh CREATE/DROP/recreate sequence. Đặt TRƯỚC khi tạo bảng vì default cột có thể NEXT VALUE FOR sequence.
    /// OnlyInSource→CREATE; OnlyInTarget→DROP (guard IF EXISTS); Different→DROP+CREATE.
    /// </summary>
    private static void AppendSequences(
        StringBuilder sb, DatabaseSchema source, DatabaseSchema target,
        List<SchemaDiffItem> sequences, SchemaScriptOptions options, bool inTx)
    {
        if (sequences.Count == 0) return;

        var emittedHeader = false;

        foreach (var item in sequences.OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase))
        {
            switch (item.Status)
            {
                case DiffStatus.OnlyInSource:
                    if (source.Sequences.TryGetValue(item.Key, out var srcNew))
                    {
                        EnsureHeader(sb, ref emittedHeader, "SEQUENCES (bước 2b)");
                        AppendCreateSequence(sb, srcNew);
                    }
                    break;

                case DiffStatus.OnlyInTarget:
                    if (options.IncludeDrops && target.Sequences.TryGetValue(item.Key, out var tgtDrop))
                    {
                        EnsureHeader(sb, ref emittedHeader, "SEQUENCES (bước 2b)");
                        AppendDropSequence(sb, tgtDrop);
                    }
                    break;

                case DiffStatus.Different:
                    // Khác biệt → DROP rồi CREATE lại theo source
                    if (source.Sequences.TryGetValue(item.Key, out var srcDiff) &&
                        target.Sequences.TryGetValue(item.Key, out var tgtDiff))
                    {
                        EnsureHeader(sb, ref emittedHeader, "SEQUENCES (bước 2b)");
                        AppendDropSequence(sb, tgtDiff);
                        AppendCreateSequence(sb, srcDiff);
                    }
                    break;

                default:
                    break; // Identical → bỏ qua
            }
        }

        if (emittedHeader) AppendSeparator(sb, inTx);
    }

    /// <summary>CREATE SEQUENCE đầy đủ thuộc tính.</summary>
    private static void AppendCreateSequence(StringBuilder sb, SequenceDef seq)
    {
        var parts = new StringBuilder();
        parts.Append($"CREATE SEQUENCE {seq.FullName} AS [{seq.DataType}]");
        if (!string.IsNullOrEmpty(seq.StartValue)) parts.Append($" START WITH {seq.StartValue}");
        if (!string.IsNullOrEmpty(seq.Increment)) parts.Append($" INCREMENT BY {seq.Increment}");
        if (!string.IsNullOrEmpty(seq.MinValue)) parts.Append($" MINVALUE {seq.MinValue}");
        if (!string.IsNullOrEmpty(seq.MaxValue)) parts.Append($" MAXVALUE {seq.MaxValue}");
        parts.Append(seq.IsCycling ? " CYCLE" : " NO CYCLE");
        parts.Append(';');
        sb.AppendLine(parts.ToString());
    }

    /// <summary>DROP SEQUENCE có guard IF EXISTS.</summary>
    private static void AppendDropSequence(StringBuilder sb, SequenceDef seq)
    {
        sb.AppendLine($"DROP SEQUENCE IF EXISTS {seq.FullName};");
    }

    // ====================== 3. CREATE TABLES (TOPOLOGICAL) ======================

    /// <summary>
    /// CREATE tables mới theo thứ tự topological dựa trên FK dependency:
    /// bảng được tham chiếu tạo trước. Chỉ tạo cột + PK, CHƯA add FK.
    /// </summary>
    private static void AppendCreateTablesTopological(StringBuilder sb, DatabaseSchema source, List<SchemaDiffItem> tableOnlyInSource, bool inTx)
    {
        if (tableOnlyInSource.Count == 0) return;

        // Tập key các bảng cần tạo (để biết dependency nào nằm trong batch này)
        var toCreate = new Dictionary<string, TableDef>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in tableOnlyInSource)
            if (source.Tables.TryGetValue(item.Key, out var t))
                toCreate[t.Key] = t;

        var ordered = TopologicalSort(toCreate);

        AppendHeader(sb, "CREATE TABLES mới (bước 3, thứ tự topological)");
        foreach (var table in ordered)
        {
            AppendCreateTable(sb, table);
            // Sau khi bảng + cột tồn tại → tạo nonclustered index (ngoài PK) kèm INCLUDE + WHERE filter
            foreach (var idx in table.Indexes
                         .Where(i => !i.IsPrimaryKey)
                         .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
                AppendCreateIndex(sb, table, idx);
            // Sau khi bảng + cột tồn tại → add extended properties từ source
            foreach (var ep in table.ExtendedProperties.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
                AppendAddExtendedProperty(sb, table, ep);
            sb.AppendLine();
        }
        AppendSeparator(sb, inTx);
    }

    /// <summary>
    /// Sắp xếp topological: bảng được tham chiếu (qua FK) đứng trước bảng tham chiếu nó.
    /// Chỉ xét FK trỏ tới bảng cũng nằm trong tập cần tạo. Self-ref / cycle: fallback theo tên.
    /// </summary>
    private static List<TableDef> TopologicalSort(Dictionary<string, TableDef> toCreate)
    {
        var result = new List<TableDef>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Duyệt theo thứ tự tên ổn định
        foreach (var key in toCreate.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            Visit(key, toCreate, visited, inStack, result);

        return result;
    }

    private static void Visit(
        string key, Dictionary<string, TableDef> toCreate,
        HashSet<string> visited, HashSet<string> inStack, List<TableDef> result)
    {
        if (visited.Contains(key)) return;
        if (inStack.Contains(key)) return; // cycle → bỏ qua cạnh này (tránh vòng lặp vô hạn)

        inStack.Add(key);
        var table = toCreate[key];

        // Thăm các bảng được tham chiếu trước (dependency)
        foreach (var fk in table.ForeignKeys)
        {
            var refKey = $"{fk.RefSchema}.{fk.RefTable}";
            if (!string.Equals(refKey, key, StringComparison.OrdinalIgnoreCase) && toCreate.ContainsKey(refKey))
                Visit(refKey, toCreate, visited, inStack, result);
        }

        inStack.Remove(key);
        if (visited.Add(key))
            result.Add(table);
    }

    /// <summary>CREATE TABLE đầy đủ cột + PK lấy từ source (chưa add FK).</summary>
    private static void AppendCreateTable(StringBuilder sb, TableDef table)
    {
        sb.AppendLine($"CREATE TABLE {table.FullName} (");

        var lines = new List<string>();
        foreach (var col in table.Columns.OrderBy(c => c.OrdinalPosition))
            lines.Add("    " + ColumnDefinitionText(col));

        var pk = table.PrimaryKey;
        if (pk != null && pk.Columns.Count > 0)
        {
            var cols = string.Join(", ", pk.Columns.Select(c => B(c)));
            var clustered = pk.IsClustered ? "CLUSTERED" : "NONCLUSTERED";
            lines.Add($"    CONSTRAINT {B(pk.Name)} PRIMARY KEY {clustered} ({cols})");
        }

        sb.AppendLine(string.Join(",\n", lines));
        sb.AppendLine(");");
    }

    // ====================== 4. ALTER TABLES ======================

    private static void AppendAlterTables(
        StringBuilder sb, DatabaseSchema source, DatabaseSchema target,
        List<SchemaDiffItem> tableDifferent, SchemaScriptOptions options, bool inTx)
    {
        if (tableDifferent.Count == 0) return;

        var emittedHeader = false;
        foreach (var item in tableDifferent)
        {
            if (!source.Tables.TryGetValue(item.Key, out var src)) continue;
            if (!target.Tables.TryGetValue(item.Key, out var tgt)) continue;

            EnsureHeader(sb, ref emittedHeader, "ALTER TABLES (bước 4)");
            AppendAlterTable(sb, src, tgt, options);
        }

        if (emittedHeader) AppendSeparator(sb, inTx);
    }

    /// <summary>Sinh ALTER TABLE ADD/ALTER/DROP COLUMN + ADD/DROP check constraint (chưa đụng FK).</summary>
    private static void AppendAlterTable(StringBuilder sb, TableDef src, TableDef tgt, SchemaScriptOptions options)
    {
        sb.AppendLine($"-- {tgt.FullName}");

        var srcCols = src.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var tgtCols = tgt.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        // Cột có ở source nhưng thiếu ở target → ADD COLUMN
        foreach (var col in src.Columns.OrderBy(c => c.OrdinalPosition))
        {
            if (!tgtCols.ContainsKey(col.Name))
                sb.AppendLine($"ALTER TABLE {tgt.FullName} ADD {ColumnDefinitionText(col)};");
        }

        // Cột có cả 2 bên nhưng khác kiểu/null → ALTER COLUMN
        foreach (var col in src.Columns.OrderBy(c => c.OrdinalPosition))
        {
            if (tgtCols.TryGetValue(col.Name, out var tc) && col.Signature() != tc.Signature())
            {
                if (col.IsComputed)
                    sb.AppendLine($"-- Column [{col.Name}] là computed column, cần DROP/ADD thủ công.");
                else
                    sb.AppendLine($"ALTER TABLE {tgt.FullName} ALTER COLUMN {AlterColumnTypeText(col)};");
            }
        }

        // Cột thừa ở target → DROP COLUMN (nếu IncludeDrops)
        if (options.IncludeDrops)
        {
            foreach (var col in tgt.Columns.OrderBy(c => c.OrdinalPosition))
            {
                if (!srcCols.ContainsKey(col.Name))
                {
                    sb.AppendLine($"-- CẢNH BÁO: DROP COLUMN [{col.Name}] sẽ mất dữ liệu cột này.");
                    sb.AppendLine($"ALTER TABLE {tgt.FullName} DROP COLUMN {B(col.Name)};");
                }
            }
        }

        // Check constraints: ADD/DROP/changed. (FK xử lý ở bước 1 + 6, không đụng ở đây.)
        var srcChk = src.CheckConstraints.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var tgtChk = tgt.CheckConstraints.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        // Check thừa ở target hoặc đổi definition → DROP trước
        foreach (var c in tgt.CheckConstraints)
        {
            var existsInSrc = srcChk.TryGetValue(c.Name, out var sc);
            var changed = existsInSrc && Normalize(sc!.Definition) != Normalize(c.Definition);
            var removed = !existsInSrc;
            if (changed || (removed && options.IncludeDrops))
            {
                sb.AppendLine($"IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = '{Q(c.Name)}' AND parent_object_id = OBJECT_ID(N'{Q(tgt.FullName)}'))");
                sb.AppendLine($"    ALTER TABLE {tgt.FullName} DROP CONSTRAINT {B(c.Name)};");
            }
        }

        // Check thiếu ở target hoặc đổi definition → ADD lại từ source
        foreach (var c in src.CheckConstraints)
        {
            var existsInTgt = tgtChk.TryGetValue(c.Name, out var tc);
            var changed = existsInTgt && Normalize(tc!.Definition) != Normalize(c.Definition);
            var added = !existsInTgt;
            if (added || changed)
                sb.AppendLine($"ALTER TABLE {tgt.FullName} ADD CONSTRAINT {B(c.Name)} CHECK {c.Definition};");
        }

        // Index (ngoài PK): ADD index thiếu/khác, DROP index thừa. So theo Signature, map theo tên.
        var srcIdx = src.Indexes.Where(i => !i.IsPrimaryKey).ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
        var tgtIdx = tgt.Indexes.Where(i => !i.IsPrimaryKey).ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);

        // Index thừa ở target hoặc đổi signature → DROP trước
        foreach (var idx in tgt.Indexes.Where(i => !i.IsPrimaryKey).OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
            var existsInSrc = srcIdx.TryGetValue(idx.Name, out var si);
            var changed = existsInSrc && si!.Signature() != idx.Signature();
            var removed = !existsInSrc;
            if (changed || (removed && options.IncludeDrops))
                AppendDropIndex(sb, tgt, idx);
        }

        // Index thiếu ở target hoặc đổi signature → CREATE lại từ source
        foreach (var idx in src.Indexes.Where(i => !i.IsPrimaryKey).OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
            var existsInTgt = tgtIdx.TryGetValue(idx.Name, out var ti);
            var changed = existsInTgt && ti!.Signature() != idx.Signature();
            var added = !existsInTgt;
            if (added || changed)
                AppendCreateIndex(sb, tgt, idx);
        }

        // Extended properties: add/update/drop phần khác biệt (sau khi cột đã tồn tại).
        AppendAlterExtendedProperties(sb, src, tgt, options);

        sb.AppendLine();
    }

    // ====================== INDEX HELPERS ======================

    /// <summary>CREATE [UNIQUE] [NONCLUSTERED|CLUSTERED] INDEX kèm INCLUDE(...) + WHERE filter (guard IF NOT EXISTS).</summary>
    private static void AppendCreateIndex(StringBuilder sb, TableDef table, IndexDef idx)
    {
        var unique = idx.IsUnique ? "UNIQUE " : "";
        var clustered = idx.IsClustered ? "CLUSTERED" : "NONCLUSTERED";
        var keyCols = string.Join(", ", idx.Columns.Select(c => B(c)));

        var stmt = new StringBuilder();
        stmt.Append($"CREATE {unique}{clustered} INDEX {B(idx.Name)} ON {table.FullName} ({keyCols})");

        // INCLUDE(...) nếu có included columns
        if (idx.IncludedColumns.Count > 0)
        {
            var inc = string.Join(", ", idx.IncludedColumns.Select(c => B(c)));
            stmt.Append($" INCLUDE ({inc})");
        }

        // WHERE filter nếu là filtered index
        if (!string.IsNullOrWhiteSpace(idx.FilterDefinition))
            stmt.Append($" WHERE {idx.FilterDefinition}");

        stmt.Append(';');

        // Guard: chỉ tạo nếu index chưa tồn tại
        sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{Q(idx.Name)}' AND object_id = OBJECT_ID(N'{Q(table.FullName)}'))");
        sb.AppendLine($"    {stmt}");
    }

    /// <summary>DROP INDEX có guard IF EXISTS.</summary>
    private static void AppendDropIndex(StringBuilder sb, TableDef table, IndexDef idx)
    {
        sb.AppendLine($"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{Q(idx.Name)}' AND object_id = OBJECT_ID(N'{Q(table.FullName)}'))");
        sb.AppendLine($"    DROP INDEX {B(idx.Name)} ON {table.FullName};");
    }

    // ====================== EXTENDED PROPERTY HELPERS ======================

    /// <summary>Sinh sp_add/sp_update/sp_dropextendedproperty cho phần khác biệt giữa src/tgt.</summary>
    private static void AppendAlterExtendedProperties(StringBuilder sb, TableDef src, TableDef tgt, SchemaScriptOptions options)
    {
        var srcExt = src.ExtendedProperties.GroupBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var tgtExt = tgt.ExtendedProperties.GroupBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Có ở source: thiếu ở target → add; khác value → update
        foreach (var e in srcExt.Values.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!tgtExt.TryGetValue(e.Key, out var te))
                AppendAddExtendedProperty(sb, tgt, e);
            else if (e.Value != te.Value)
                AppendUpdateExtendedProperty(sb, tgt, e);
        }

        // Thừa ở target → drop (nếu IncludeDrops)
        if (options.IncludeDrops)
        {
            foreach (var e in tgtExt.Values.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (!srcExt.ContainsKey(e.Key))
                    AppendDropExtendedProperty(sb, tgt, e);
            }
        }
    }

    /// <summary>EXEC sys.sp_addextendedproperty cho 1 property (table hoặc column).</summary>
    private static void AppendAddExtendedProperty(StringBuilder sb, TableDef table, ExtendedPropertyDef ep)
    {
        sb.AppendLine($"EXEC sys.sp_addextendedproperty {ExtendedPropertyArgs(table, ep, includeValue: true)};");
    }

    /// <summary>EXEC sys.sp_updateextendedproperty cho 1 property.</summary>
    private static void AppendUpdateExtendedProperty(StringBuilder sb, TableDef table, ExtendedPropertyDef ep)
    {
        sb.AppendLine($"EXEC sys.sp_updateextendedproperty {ExtendedPropertyArgs(table, ep, includeValue: true)};");
    }

    /// <summary>EXEC sys.sp_dropextendedproperty cho 1 property (không kèm @value).</summary>
    private static void AppendDropExtendedProperty(StringBuilder sb, TableDef table, ExtendedPropertyDef ep)
    {
        sb.AppendLine($"EXEC sys.sp_dropextendedproperty {ExtendedPropertyArgs(table, ep, includeValue: false)};");
    }

    /// <summary>
    /// Dựng danh sách tham số cho sp_*extendedproperty:
    /// @name, [@value], @level0type='SCHEMA', @level0name, @level1type='TABLE', @level1name,
    /// và @level2type='COLUMN', @level2name nếu là property cấp column.
    /// </summary>
    private static string ExtendedPropertyArgs(TableDef table, ExtendedPropertyDef ep, bool includeValue)
    {
        var args = new StringBuilder();
        args.Append($"@name = N'{EscapeSql(ep.Name)}'");
        if (includeValue)
            args.Append($", @value = N'{EscapeSql(ep.Value)}'");
        args.Append($", @level0type = N'SCHEMA', @level0name = N'{EscapeSql(table.Schema)}'");
        args.Append($", @level1type = N'TABLE', @level1name = N'{EscapeSql(table.Name)}'");
        if (ep.Level == "COLUMN" && !string.IsNullOrEmpty(ep.ColumnName))
            args.Append($", @level2type = N'COLUMN', @level2name = N'{EscapeSql(ep.ColumnName)}'");
        return args.ToString();
    }

    /// <summary>Escape dấu nháy đơn cho chuỗi literal SQL.</summary>
    private static string EscapeSql(string s) => (s ?? "").Replace("'", "''");

    /// <summary>Escape nháy đơn cho tên object nhúng vào literal '...' (alias của EscapeSql).</summary>
    private static string Q(string s) => (s ?? "").Replace("'", "''");

    /// <summary>Bọc identifier trong [] và escape ']' → ']]' để chống vỡ/injection.</summary>
    private static string B(string s) => "[" + (s ?? "").Replace("]", "]]") + "]";

    // ====================== 5. PROGRAMMABLES ======================

    /// <summary>
    /// CREATE OR ALTER programmable theo source. Nhóm theo thứ tự View -> Function -> SP -> Trigger.
    /// inTx = true: dùng EXEC(N'...') để không cần GO (giữ trong 1 transaction).
    /// inTx = false: dùng GO chia batch.
    /// </summary>
    private static void AppendProgrammables(
        StringBuilder sb, DatabaseSchema source, DatabaseSchema target,
        List<SchemaDiffItem> programmables, bool inTx)
    {
        // Chỉ OnlyInSource + Different cần create/alter (OnlyInTarget đã drop ở bước 2)
        var toApply = programmables.Where(i => i.Status == DiffStatus.OnlyInSource || i.Status == DiffStatus.Different).ToList();
        if (toApply.Count == 0) return;

        AppendHeader(sb, "CREATE OR ALTER PROGRAMMABLES (bước 5)");

        // Thứ tự nhóm: View -> Function -> SP -> Trigger
        var order = new[] { DbObjectType.View, DbObjectType.Function, DbObjectType.StoredProcedure, DbObjectType.Trigger };

        // Counter để đặt tên biến @sql_<n> DUY NHẤT (cùng batch không có GO → không được trùng tên biến).
        var sqlVarCounter = 0;

        foreach (var type in order)
        {
            var group = toApply
                .Where(i => i.ObjectType == type)
                .OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var item in group)
            {
                if (!source.Programmables.TryGetValue(item.Key, out var src)) continue;
                AppendProgrammableBody(sb, src, inTx, ref sqlVarCounter);
            }
        }

        AppendSeparator(sb, inTx);
    }

    /// <summary>In 1 programmable bằng CREATE OR ALTER. Wrap EXEC(N'...') khi inTx, ngược lại dùng GO.</summary>
    private static void AppendProgrammableBody(StringBuilder sb, ProgrammableObject src, bool inTx, ref int sqlVarCounter)
    {
        var createOrAlter = ToCreateOrAlter(src.Definition);

        if (inTx)
        {
            // BUG 2: literal nvarchar > 4000 ký tự bị cắt → không dùng 1 EXEC(N'...').
            // Thay bằng biến nvarchar(max) tích lũy, tên @sql_<n> duy nhất theo counter,
            // gán bằng nhiều SET += để mỗi literal đều < 4000 ký tự.
            var varName = $"@sql_{sqlVarCounter++}";
            sb.AppendLine($"-- {src.FullName}");
            AppendAccumulatedExec(sb, varName, createOrAlter);
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine($"-- {src.FullName}");
            sb.AppendLine(createOrAlter.TrimEnd());
            sb.AppendLine("GO");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Sinh biến nvarchar(max) tích lũy rồi EXEC sys.sp_executesql, để né giới hạn 4000 ký tự của literal N'...'.
    /// Chia chuỗi GỐC (chưa escape) thành các đoạn &lt;= 2000 ký tự, RỒI escape ' → '' TỪNG đoạn
    /// (tránh cắt ngang cặp ''). Đoạn đầu DECLARE = N'...', các đoạn sau SET varName += N'...'.
    /// </summary>
    private static void AppendAccumulatedExec(StringBuilder sb, string varName, string definition)
    {
        const int chunkSize = 2000;
        var text = definition ?? "";

        // Chia theo độ dài chuỗi gốc (chưa escape) để mỗi đoạn an toàn dưới 4000 sau khi escape.
        var chunks = new List<string>();
        for (var i = 0; i < text.Length; i += chunkSize)
            chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
        if (chunks.Count == 0) chunks.Add(""); // definition rỗng → vẫn declare biến rỗng

        for (var i = 0; i < chunks.Count; i++)
        {
            var piece = chunks[i].Replace("'", "''");
            if (i == 0)
                sb.AppendLine($"DECLARE {varName} nvarchar(max) = N'{piece}';");
            else
                sb.AppendLine($"SET {varName} += N'{piece}';");
        }

        sb.AppendLine($"EXEC sys.sp_executesql {varName};");
    }

    /// <summary>Đổi definition "CREATE ..." thành "CREATE OR ALTER ..." (nếu chưa có).</summary>
    private static string ToCreateOrAlter(string definition)
    {
        var def = definition.TrimStart();
        var idx = def.IndexOf("CREATE", StringComparison.OrdinalIgnoreCase);

        if (idx < 0)
            return def.TrimEnd(); // không bắt đầu bằng CREATE → trả nguyên (an toàn)

        // Đã là "CREATE OR ALTER" rồi thì không đụng
        var after = def.Substring(idx + "CREATE".Length).TrimStart();
        if (after.StartsWith("OR ALTER", StringComparison.OrdinalIgnoreCase))
            return def.TrimEnd();

        return (def.Substring(0, idx) + "CREATE OR ALTER" + def.Substring(idx + "CREATE".Length)).TrimEnd();
    }

    // ====================== 6. ADD FOREIGN KEYS ======================

    /// <summary>
    /// ADD tất cả FK SAU CÙNG: FK của bảng mới (OnlyInSource) + FK thiếu/khác ở bảng Different.
    /// </summary>
    private static void AppendAddForeignKeys(
        StringBuilder sb, DatabaseSchema source, DatabaseSchema target,
        List<SchemaDiffItem> tableOnlyInSource, List<SchemaDiffItem> tableDifferent, bool inTx)
    {
        var emittedHeader = false;

        // FK cho bảng mới: add toàn bộ FK của source
        foreach (var item in tableOnlyInSource)
        {
            if (!source.Tables.TryGetValue(item.Key, out var src)) continue;
            foreach (var fk in src.ForeignKeys)
            {
                EnsureHeader(sb, ref emittedHeader, "ADD FOREIGN KEYS (bước 6)");
                AppendAddFkStmt(sb, src, fk);
            }
        }

        // FK cho bảng Different: add FK thiếu (không có ở target) hoặc khác (đã drop ở bước 1)
        foreach (var item in tableDifferent)
        {
            if (!source.Tables.TryGetValue(item.Key, out var src)) continue;
            if (!target.Tables.TryGetValue(item.Key, out var tgt)) continue;

            var tgtFk = tgt.ForeignKeys.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var fk in src.ForeignKeys)
            {
                var exists = tgtFk.TryGetValue(fk.Name, out var tfk);
                var changed = exists && tfk!.Signature() != fk.Signature();
                var missing = !exists;

                if (missing || changed)
                {
                    EnsureHeader(sb, ref emittedHeader, "ADD FOREIGN KEYS (bước 6)");
                    AppendAddFkStmt(sb, src, fk);
                }
            }
        }

        if (emittedHeader) AppendSeparator(sb, inTx);
    }

    /// <summary>Câu ALTER TABLE ADD CONSTRAINT FOREIGN KEY, có guard IF NOT EXISTS.</summary>
    private static void AppendAddFkStmt(StringBuilder sb, TableDef table, ForeignKeyDef fk)
    {
        var cols = string.Join(", ", fk.Columns.Select(c => B(c)));
        var refCols = string.Join(", ", fk.RefColumns.Select(c => B(c)));
        var refTable = $"{B(fk.RefSchema)}.{B(fk.RefTable)}";

        var onDelete = ReferentialActionText(fk.OnDeleteAction);
        var onUpdate = ReferentialActionText(fk.OnUpdateAction);
        var actions = "";
        if (!string.IsNullOrEmpty(onDelete)) actions += $" ON DELETE {onDelete}";
        if (!string.IsNullOrEmpty(onUpdate)) actions += $" ON UPDATE {onUpdate}";

        // Guard: chỉ add nếu FK chưa tồn tại
        sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{Q(fk.Name)}' AND parent_object_id = OBJECT_ID(N'{Q(table.FullName)}'))");
        sb.AppendLine($"    ALTER TABLE {table.FullName} ADD CONSTRAINT {B(fk.Name)} FOREIGN KEY ({cols}) REFERENCES {refTable} ({refCols}){actions};");
    }

    /// <summary>Đổi action desc (NO_ACTION/CASCADE/SET_NULL/SET_DEFAULT) thành cú pháp SQL. NO_ACTION = "" (mặc định).</summary>
    private static string ReferentialActionText(string action) => (action ?? "").ToUpperInvariant() switch
    {
        "CASCADE" => "CASCADE",
        "SET_NULL" => "SET NULL",
        "SET_DEFAULT" => "SET DEFAULT",
        _ => "", // NO_ACTION → bỏ qua (mặc định của SQL Server)
    };

    // ====================== SECURITY: ROLES ======================

    /// <summary>
    /// CREATE ROLE (OnlyInSource) / DROP ROLE (OnlyInTarget) — guard qua DATABASE_PRINCIPAL_ID.
    /// Đặt sớm để user có thể được gán membership / cấp quyền sau đó.
    /// </summary>
    private static void AppendRoles(StringBuilder sb, List<SchemaDiffItem> roles, SchemaScriptOptions options, bool inTx, List<string> warnings)
    {
        if (roles.Count == 0) return;

        var emittedHeader = false;

        // CREATE trước
        foreach (var item in roles.Where(i => i.Status == DiffStatus.OnlyInSource)
                     .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
            EnsureHeader(sb, ref emittedHeader, "ROLES (bước 0a)");
            sb.AppendLine($"IF DATABASE_PRINCIPAL_ID('{EscapeSql(item.Name)}') IS NULL");
            sb.AppendLine($"    CREATE ROLE {B(item.Name)};");
        }

        // DROP role thừa (nếu IncludeDrops)
        if (options.IncludeDrops)
        {
            foreach (var item in roles.Where(i => i.Status == DiffStatus.OnlyInTarget)
                         .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
            {
                EnsureHeader(sb, ref emittedHeader, "ROLES (bước 0a)");
                sb.AppendLine($"-- CẢNH BÁO: DROP ROLE [{item.Name}] chỉ chạy được khi role không còn member.");
                sb.AppendLine($"IF DATABASE_PRINCIPAL_ID('{EscapeSql(item.Name)}') IS NOT NULL");
                sb.AppendLine($"    DROP ROLE {B(item.Name)};");
                warnings.Add($"Role drop: ROLE [{item.Name}] sẽ bị xóa (chỉ chạy được khi không còn member).");
            }
        }

        if (emittedHeader) AppendSeparator(sb, inTx);
    }

    // ====================== SECURITY: USERS ======================

    /// <summary>
    /// CREATE USER (OnlyInSource) / ALTER USER (Different, default schema) / DROP USER (OnlyInTarget).
    /// AuthenticationDesc='NONE' hoặc không map login → WITHOUT LOGIN; ngược lại FOR LOGIN [name].
    /// </summary>
    private static void AppendUsers(
        StringBuilder sb, DatabaseSchema source, DatabaseSchema target,
        List<SchemaDiffItem> users, SchemaScriptOptions options, bool inTx)
    {
        if (users.Count == 0) return;

        var emittedHeader = false;

        foreach (var item in users.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
            switch (item.Status)
            {
                case DiffStatus.OnlyInSource:
                    if (source.Users.TryGetValue(item.Name, out var srcNew))
                    {
                        EnsureHeader(sb, ref emittedHeader, "USERS (bước 0a)");
                        AppendCreateUser(sb, srcNew);
                    }
                    break;

                case DiffStatus.Different:
                    // Khác biệt (vd default schema) → ALTER USER theo source
                    if (source.Users.TryGetValue(item.Name, out var srcDiff))
                    {
                        EnsureHeader(sb, ref emittedHeader, "USERS (bước 0a)");
                        if (!string.IsNullOrEmpty(srcDiff.DefaultSchema))
                            sb.AppendLine($"ALTER USER {B(srcDiff.Name)} WITH DEFAULT_SCHEMA={B(srcDiff.DefaultSchema)};");
                    }
                    break;

                case DiffStatus.OnlyInTarget:
                    if (options.IncludeDrops && target.Users.TryGetValue(item.Name, out var tgtDrop))
                    {
                        EnsureHeader(sb, ref emittedHeader, "USERS (bước 0a)");
                        sb.AppendLine($"IF DATABASE_PRINCIPAL_ID('{EscapeSql(tgtDrop.Name)}') IS NOT NULL");
                        sb.AppendLine($"    DROP USER {B(tgtDrop.Name)};");
                    }
                    break;

                default:
                    break; // Identical → bỏ qua
            }
        }

        if (emittedHeader) AppendSeparator(sb, inTx);
    }

    /// <summary>CREATE USER có guard IF NULL; WITHOUT LOGIN nếu auth='NONE', else FOR LOGIN; kèm DEFAULT_SCHEMA.</summary>
    private static void AppendCreateUser(StringBuilder sb, DbUserDef user)
    {
        // NONE / rỗng → user không map login (WITHOUT LOGIN); ngược lại map login cùng tên
        var withoutLogin = string.IsNullOrEmpty(user.AuthenticationDesc) ||
                           string.Equals(user.AuthenticationDesc, "NONE", StringComparison.OrdinalIgnoreCase);

        var stmt = new StringBuilder();
        stmt.Append($"CREATE USER {B(user.Name)}");
        stmt.Append(withoutLogin ? " WITHOUT LOGIN" : $" FOR LOGIN {B(user.Name)}");
        if (!string.IsNullOrEmpty(user.DefaultSchema))
            stmt.Append($" WITH DEFAULT_SCHEMA={B(user.DefaultSchema)}");
        stmt.Append(';');

        sb.AppendLine($"IF DATABASE_PRINCIPAL_ID('{EscapeSql(user.Name)}') IS NULL");
        sb.AppendLine($"    {stmt}");
    }

    // ====================== SECURITY: ROLE MEMBERSHIPS ======================

    /// <summary>
    /// Gán role membership cho user phía source (ADD MEMBER). Áp dụng cho user mới hoặc Different.
    /// Guard IS_ROLEMEMBER để idempotent.
    /// </summary>
    private static void AppendRoleMemberships(StringBuilder sb, DatabaseSchema source, List<SchemaDiffItem> users, bool inTx)
    {
        // Chỉ xét user còn tồn tại ở source (OnlyInSource + Different)
        var applicable = users
            .Where(i => i.Status == DiffStatus.OnlyInSource || i.Status == DiffStatus.Different)
            .ToList();
        if (applicable.Count == 0) return;

        var emittedHeader = false;

        foreach (var item in applicable.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!source.Users.TryGetValue(item.Name, out var user)) continue;
            foreach (var role in user.Roles.OrderBy(r => r, StringComparer.OrdinalIgnoreCase))
            {
                EnsureHeader(sb, ref emittedHeader, "ROLE MEMBERSHIPS (bước 0a)");
                sb.AppendLine($"IF IS_ROLEMEMBER('{EscapeSql(role)}', '{EscapeSql(user.Name)}') = 0");
                sb.AppendLine($"    ALTER ROLE {B(role)} ADD MEMBER {B(user.Name)};");
            }
        }

        if (emittedHeader) AppendSeparator(sb, inTx);
    }

    // ====================== SECURITY: PERMISSIONS ======================

    /// <summary>
    /// GRANT/DENY permission (OnlyInSource) + REVOKE (OnlyInTarget). Đặt GẦN CUỐI để object đã tồn tại.
    /// ON theo ClassDesc: DATABASE→không ON; SCHEMA→ON SCHEMA::[s]; OBJECT→ON [s].[o].
    /// </summary>
    private static void AppendPermissions(
        StringBuilder sb, DatabaseSchema source, DatabaseSchema target,
        List<SchemaDiffItem> permissions, SchemaScriptOptions options, bool inTx)
    {
        if (permissions.Count == 0) return;

        // Lookup def thực theo Key (Name = "$"+Key)
        var srcMap = source.Permissions.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var tgtMap = target.Permissions.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var emittedHeader = false;

        foreach (var item in permissions.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
            var key = item.Name.StartsWith("$") ? item.Name.Substring(1) : item.Name;

            switch (item.Status)
            {
                case DiffStatus.OnlyInSource:
                    if (srcMap.TryGetValue(key, out var grant))
                    {
                        EnsureHeader(sb, ref emittedHeader, "PERMISSIONS (bước 7 — gần cuối)");
                        AppendGrantPermission(sb, grant);
                    }
                    break;

                case DiffStatus.OnlyInTarget:
                    if (options.IncludeDrops && tgtMap.TryGetValue(key, out var revoke))
                    {
                        EnsureHeader(sb, ref emittedHeader, "PERMISSIONS (bước 7 — gần cuối)");
                        AppendRevokePermission(sb, revoke);
                    }
                    break;

                default:
                    break; // Identical → bỏ qua
            }
        }

        if (emittedHeader) AppendSeparator(sb, inTx);
    }

    /// <summary>GRANT/DENY perm [ON ...] TO [grantee] [WITH GRANT OPTION].</summary>
    private static void AppendGrantPermission(StringBuilder sb, DbPermissionDef p)
    {
        var on = PermissionOnClause(p);
        // state GRANT_WITH_GRANT_OPTION → GRANT ... WITH GRANT OPTION; DENY → DENY; còn lại → GRANT
        var withGrant = string.Equals(p.StateDesc, "GRANT_WITH_GRANT_OPTION", StringComparison.OrdinalIgnoreCase);
        var verb = string.Equals(p.StateDesc, "DENY", StringComparison.OrdinalIgnoreCase) ? "DENY" : "GRANT";
        var tail = withGrant ? " WITH GRANT OPTION" : "";
        sb.AppendLine($"{verb} {p.PermissionName}{on} TO {B(p.Grantee)}{tail};");
    }

    /// <summary>REVOKE perm [ON ...] FROM [grantee].</summary>
    private static void AppendRevokePermission(StringBuilder sb, DbPermissionDef p)
    {
        var on = PermissionOnClause(p);
        sb.AppendLine($"REVOKE {p.PermissionName}{on} FROM {B(p.Grantee)};");
    }

    /// <summary>Dựng mệnh đề ON theo ClassDesc: DATABASE→""; SCHEMA→" ON SCHEMA::[s]"; OBJECT→" ON [s].[o]".</summary>
    private static string PermissionOnClause(DbPermissionDef p)
    {
        if (string.Equals(p.ClassDesc, "SCHEMA", StringComparison.OrdinalIgnoreCase))
            // TargetName đã ở dạng "[schema]" → ON SCHEMA::[schema]
            return $" ON SCHEMA::{p.TargetName}";
        if (string.Equals(p.ClassDesc, "OBJECT_OR_COLUMN", StringComparison.OrdinalIgnoreCase))
            // TargetName đã ở dạng "[schema].[obj]" → ON [schema].[obj]
            return string.IsNullOrEmpty(p.TargetName) ? "" : $" ON {p.TargetName}";
        // DATABASE hoặc class khác → không có ON
        return "";
    }

    // ====================== PARTITION: FUNCTIONS ======================

    /// <summary>
    /// CREATE/DROP/recreate partition function. Đặt TRƯỚC CREATE TABLE (scheme tham chiếu function).
    /// OnlyInSource→CREATE; OnlyInTarget→DROP (guard); Different→DROP+CREATE (cảnh báo).
    /// </summary>
    private static void AppendPartitionFunctions(
        StringBuilder sb, DatabaseSchema source, DatabaseSchema target,
        List<SchemaDiffItem> partitionFunctions, SchemaScriptOptions options, bool inTx)
    {
        if (partitionFunctions.Count == 0) return;

        var emittedHeader = false;

        foreach (var item in partitionFunctions.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
            switch (item.Status)
            {
                case DiffStatus.OnlyInSource:
                    if (source.PartitionFunctions.TryGetValue(item.Name, out var srcNew))
                    {
                        EnsureHeader(sb, ref emittedHeader, "PARTITION FUNCTIONS (bước 0b)");
                        AppendCreatePartitionFunction(sb, srcNew);
                    }
                    break;

                case DiffStatus.OnlyInTarget:
                    if (options.IncludeDrops && target.PartitionFunctions.TryGetValue(item.Name, out var tgtDrop))
                    {
                        EnsureHeader(sb, ref emittedHeader, "PARTITION FUNCTIONS (bước 0b)");
                        AppendDropPartitionFunction(sb, tgtDrop);
                    }
                    break;

                case DiffStatus.Different:
                    if (source.PartitionFunctions.TryGetValue(item.Name, out var srcDiff) &&
                        target.PartitionFunctions.TryGetValue(item.Name, out var tgtDiff))
                    {
                        EnsureHeader(sb, ref emittedHeader, "PARTITION FUNCTIONS (bước 0b)");
                        sb.AppendLine($"-- CẢNH BÁO: thay đổi partition function [{tgtDiff.Name}] → DROP + CREATE (phải bỏ scheme/table phụ thuộc trước).");
                        AppendDropPartitionFunction(sb, tgtDiff);
                        AppendCreatePartitionFunction(sb, srcDiff);
                    }
                    break;

                default:
                    break; // Identical → bỏ qua
            }
        }

        if (emittedHeader) AppendSeparator(sb, inTx);
    }

    /// <summary>CREATE PARTITION FUNCTION [name]([type]) AS RANGE LEFT|RIGHT FOR VALUES (...).</summary>
    private static void AppendCreatePartitionFunction(StringBuilder sb, PartitionFunctionDef pf)
    {
        var range = pf.RangeRight ? "RIGHT" : "LEFT";
        // BUG 3: boundary kiểu chuỗi/ngày/guid phải bọc nháy đơn (escape '); kiểu số để raw.
        var values = string.Join(", ", pf.BoundaryValues.Select(v => FormatBoundaryLiteral(pf.ParameterType, v)));
        sb.AppendLine($"CREATE PARTITION FUNCTION {B(pf.Name)} ({pf.ParameterType}) AS RANGE {range} FOR VALUES ({values});");
    }

    /// <summary>
    /// Format 1 boundary value của partition function theo ParameterType:
    /// char/varchar/nchar/nvarchar/date/time/datetime/datetime2/datetimeoffset/uniqueidentifier
    /// → bọc nháy đơn '...' (escape nháy); kiểu số (int/bigint/decimal/...) → raw.
    /// </summary>
    private static string FormatBoundaryLiteral(string parameterType, string value)
    {
        // ParameterType có thể kèm độ dài: "varchar(10)" → lấy phần tên kiểu trước '('
        var baseType = parameterType ?? "";
        var paren = baseType.IndexOf('(');
        if (paren >= 0) baseType = baseType.Substring(0, paren);
        baseType = baseType.Trim().ToLowerInvariant();

        var needQuote = baseType switch
        {
            "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext"
                or "date" or "time" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset"
                or "uniqueidentifier" => true,
            _ => false,
        };

        return needQuote ? $"'{Q(value)}'" : value;
    }

    /// <summary>DROP PARTITION FUNCTION có guard IF EXISTS.</summary>
    private static void AppendDropPartitionFunction(StringBuilder sb, PartitionFunctionDef pf)
    {
        sb.AppendLine($"IF EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = '{EscapeSql(pf.Name)}')");
        sb.AppendLine($"    DROP PARTITION FUNCTION {B(pf.Name)};");
    }

    // ====================== PARTITION: SCHEMES ======================

    /// <summary>
    /// CREATE/DROP/recreate partition scheme. Đặt SAU function, TRƯỚC CREATE TABLE.
    /// OnlyInSource→CREATE; OnlyInTarget→DROP (guard); Different→DROP+CREATE (cảnh báo).
    /// </summary>
    private static void AppendPartitionSchemes(
        StringBuilder sb, DatabaseSchema source, DatabaseSchema target,
        List<SchemaDiffItem> partitionSchemes, SchemaScriptOptions options, bool inTx)
    {
        if (partitionSchemes.Count == 0) return;

        var emittedHeader = false;

        foreach (var item in partitionSchemes.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
            switch (item.Status)
            {
                case DiffStatus.OnlyInSource:
                    if (source.PartitionSchemes.TryGetValue(item.Name, out var srcNew))
                    {
                        EnsureHeader(sb, ref emittedHeader, "PARTITION SCHEMES (bước 0b)");
                        AppendCreatePartitionScheme(sb, srcNew);
                    }
                    break;

                case DiffStatus.OnlyInTarget:
                    if (options.IncludeDrops && target.PartitionSchemes.TryGetValue(item.Name, out var tgtDrop))
                    {
                        EnsureHeader(sb, ref emittedHeader, "PARTITION SCHEMES (bước 0b)");
                        AppendDropPartitionScheme(sb, tgtDrop);
                    }
                    break;

                case DiffStatus.Different:
                    if (source.PartitionSchemes.TryGetValue(item.Name, out var srcDiff) &&
                        target.PartitionSchemes.TryGetValue(item.Name, out var tgtDiff))
                    {
                        EnsureHeader(sb, ref emittedHeader, "PARTITION SCHEMES (bước 0b)");
                        sb.AppendLine($"-- CẢNH BÁO: thay đổi partition scheme [{tgtDiff.Name}] → DROP + CREATE (phải bỏ table phụ thuộc trước).");
                        AppendDropPartitionScheme(sb, tgtDiff);
                        AppendCreatePartitionScheme(sb, srcDiff);
                    }
                    break;

                default:
                    break; // Identical → bỏ qua
            }
        }

        if (emittedHeader) AppendSeparator(sb, inTx);
    }

    /// <summary>CREATE PARTITION SCHEME [name] AS PARTITION [func] TO ([fg1],[fg2],...).</summary>
    private static void AppendCreatePartitionScheme(StringBuilder sb, PartitionSchemeDef ps)
    {
        var fgs = string.Join(", ", ps.Filegroups.Select(f => B(f)));
        sb.AppendLine($"CREATE PARTITION SCHEME {B(ps.Name)} AS PARTITION {B(ps.FunctionName)} TO ({fgs});");
    }

    /// <summary>DROP PARTITION SCHEME có guard IF EXISTS.</summary>
    private static void AppendDropPartitionScheme(StringBuilder sb, PartitionSchemeDef ps)
    {
        sb.AppendLine($"IF EXISTS (SELECT 1 FROM sys.partition_schemes WHERE name = '{EscapeSql(ps.Name)}')");
        sb.AppendLine($"    DROP PARTITION SCHEME {B(ps.Name)};");
    }

    // ====================== SCHEMAS ======================

    /// <summary>
    /// CREATE SCHEMA (OnlyInSource) đặt RẤT SỚM trước mọi object dùng schema đó.
    /// DROP SCHEMA (OnlyInTarget) có guard + cảnh báo (schema phải rỗng mới drop được).
    /// </summary>
    private static void AppendSchemas(StringBuilder sb, List<SchemaDiffItem> schemas, SchemaScriptOptions options, bool inTx, List<string> warnings)
    {
        if (schemas.Count == 0) return;

        var emittedHeader = false;

        // CREATE trước
        foreach (var item in schemas.Where(i => i.Status == DiffStatus.OnlyInSource)
                     .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
            EnsureHeader(sb, ref emittedHeader, "SCHEMAS (bước 0 — tạo sớm nhất)");
            // identifier nằm trong EXEC(N'...') → escape ] cho bracket, rồi escape ' cho literal ngoài
            sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{Q(item.Name)}')");
            sb.AppendLine($"    EXEC(N'CREATE SCHEMA {Q(B(item.Name))}');");
        }

        // DROP schema thừa (nếu IncludeDrops) — guard, cảnh báo
        if (options.IncludeDrops)
        {
            foreach (var item in schemas.Where(i => i.Status == DiffStatus.OnlyInTarget)
                         .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
            {
                EnsureHeader(sb, ref emittedHeader, "SCHEMAS (bước 0 — tạo sớm nhất)");
                sb.AppendLine($"-- CẢNH BÁO: DROP SCHEMA [{item.Name}] chỉ chạy được khi schema đã rỗng (không còn object).");
                sb.AppendLine($"IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{Q(item.Name)}')");
                sb.AppendLine($"    EXEC(N'DROP SCHEMA {Q(B(item.Name))}');");
                warnings.Add($"Schema drop: SCHEMA [{item.Name}] sẽ bị xóa (chỉ chạy được khi schema rỗng).");
            }
        }

        if (emittedHeader) AppendSeparator(sb, inTx);
    }

    // ====================== SYNONYMS ======================

    /// <summary>
    /// CREATE/DROP/recreate synonym. OnlyInSource→CREATE; OnlyInTarget→DROP (guard);
    /// Different→DROP+CREATE.
    /// </summary>
    private static void AppendSynonyms(
        StringBuilder sb, DatabaseSchema source, DatabaseSchema target,
        List<SchemaDiffItem> synonyms, SchemaScriptOptions options, bool inTx)
    {
        if (synonyms.Count == 0) return;

        var emittedHeader = false;

        foreach (var item in synonyms.OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase))
        {
            switch (item.Status)
            {
                case DiffStatus.OnlyInSource:
                    if (source.Synonyms.TryGetValue(item.Key, out var srcNew))
                    {
                        EnsureHeader(sb, ref emittedHeader, "SYNONYMS (bước 2c)");
                        AppendCreateSynonym(sb, srcNew);
                    }
                    break;

                case DiffStatus.OnlyInTarget:
                    if (options.IncludeDrops && target.Synonyms.TryGetValue(item.Key, out var tgtDrop))
                    {
                        EnsureHeader(sb, ref emittedHeader, "SYNONYMS (bước 2c)");
                        AppendDropSynonym(sb, tgtDrop);
                    }
                    break;

                case DiffStatus.Different:
                    // Synonym khác base object → DROP rồi CREATE lại theo source
                    if (source.Synonyms.TryGetValue(item.Key, out var srcDiff) &&
                        target.Synonyms.TryGetValue(item.Key, out var tgtDiff))
                    {
                        EnsureHeader(sb, ref emittedHeader, "SYNONYMS (bước 2c)");
                        AppendDropSynonym(sb, tgtDiff);
                        AppendCreateSynonym(sb, srcDiff);
                    }
                    break;

                default:
                    break; // Identical → bỏ qua
            }
        }

        if (emittedHeader) AppendSeparator(sb, inTx);
    }

    /// <summary>CREATE SYNONYM ... FOR base_object (guard IF NOT EXISTS).</summary>
    private static void AppendCreateSynonym(StringBuilder sb, SynonymDef syn)
    {
        sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.synonyms WHERE name = '{Q(syn.Name)}' AND schema_id = SCHEMA_ID('{Q(syn.Schema)}'))");
        // BaseObject có thể là tên nhiều phần ([db].[schema].[obj]) → giữ raw để không làm vỡ.
        sb.AppendLine($"    CREATE SYNONYM {syn.FullName} FOR {syn.BaseObject};");
    }

    /// <summary>DROP SYNONYM có guard IF EXISTS.</summary>
    private static void AppendDropSynonym(StringBuilder sb, SynonymDef syn)
    {
        sb.AppendLine($"DROP SYNONYM IF EXISTS {syn.FullName};");
    }

    // ====================== WARNING ENGINE ======================

    /// <summary>
    /// Thu thập cảnh báo dựa trên diff: data loss (thu nhỏ cột), object drop (DROP TABLE),
    /// column drop, dependency break (programmable tham chiếu bảng/cột đang đổi).
    /// </summary>
    private static void CollectWarnings(
        DatabaseSchema source, DatabaseSchema target,
        List<SchemaDiffItem> tableOnlyInTarget, List<SchemaDiffItem> tableDifferent,
        SchemaScriptOptions options, List<string> warnings)
    {
        // Tập tên bảng (FullName) sẽ bị thay đổi → dùng để phát hiện dependency break
        var changedTables = new List<TableDef>();

        // OBJECT DROP: mọi DROP TABLE
        if (options.IncludeDrops)
        {
            foreach (var item in tableOnlyInTarget)
            {
                if (!target.Tables.TryGetValue(item.Key, out var tgt)) continue;
                warnings.Add($"Object drop: TABLE {tgt.FullName} sẽ bị xóa (mất data).");
                changedTables.Add(tgt);
            }
        }

        // ALTER TABLE: data loss + column drop
        foreach (var item in tableDifferent)
        {
            if (!source.Tables.TryGetValue(item.Key, out var src)) continue;
            if (!target.Tables.TryGetValue(item.Key, out var tgt)) continue;

            changedTables.Add(tgt);

            var srcCols = src.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
            var tgtCols = tgt.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            // DATA LOSS: cột tồn tại cả 2 bên nhưng kiểu source HẸP HƠN target
            foreach (var sc in src.Columns)
            {
                if (tgtCols.TryGetValue(sc.Name, out var tc) && IsNarrowing(tc, sc))
                    warnings.Add($"Possible data loss: {tgt.FullName}.[{sc.Name}] {tc.FullTypeText()} -> {sc.FullTypeText()}");
            }

            // BUG 5: ADD COLUMN NOT NULL không default trên bảng đã có data sẽ lỗi.
            foreach (var sc in src.Columns)
            {
                var isAdded = !tgtCols.ContainsKey(sc.Name);
                if (isAdded && !sc.IsNullable && !sc.IsComputed &&
                    string.IsNullOrWhiteSpace(sc.DefaultDefinition))
                    warnings.Add($"Add NOT NULL column without default: {tgt.FullName}.[{sc.Name}] sẽ lỗi nếu bảng đã có dữ liệu.");
            }

            // COLUMN DROP: cột thừa ở target bị xóa (nếu IncludeDrops)
            if (options.IncludeDrops)
            {
                foreach (var tc in tgt.Columns)
                {
                    if (!srcCols.ContainsKey(tc.Name))
                        warnings.Add($"Column drop: {tgt.FullName}.[{tc.Name}] sẽ bị xóa.");
                }
            }
        }

        // DEPENDENCY BREAK: programmable (view/proc/function/trigger) ở source/target tham chiếu tên bảng đang đổi
        if (changedTables.Count > 0)
            CollectDependencyBreaks(source, target, changedTables, warnings);
    }

    /// <summary>Phát hiện programmable tham chiếu (thô, theo tên) tới bảng đang bị thay đổi → cảnh báo.</summary>
    private static void CollectDependencyBreaks(
        DatabaseSchema source, DatabaseSchema target, List<TableDef> changedTables, List<string> warnings)
    {
        // Gộp programmable 2 bên (theo Key, ưu tiên source) để quét definition
        var progs = new Dictionary<string, ProgrammableObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in target.Programmables.Values) progs[p.Key] = p;
        foreach (var p in source.Programmables.Values) progs[p.Key] = p;

        // Tránh cảnh báo trùng (1 programmable + 1 bảng chỉ báo 1 lần)
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tbl in changedTables)
        {
            foreach (var prog in progs.Values)
            {
                if (string.IsNullOrEmpty(prog.Definition)) continue;
                // Tìm thô: definition chứa tên bảng (cả dạng [Name] lẫn Name trần)
                if (ReferencesTable(prog.Definition, tbl))
                {
                    var dupKey = $"{prog.Key}|{tbl.Key}";
                    if (seen.Add(dupKey))
                        warnings.Add($"Dependency break: {prog.FullName} dùng {tbl.FullName} đang bị thay đổi.");
                }
            }
        }
    }

    /// <summary>Tìm thô tên bảng trong definition: khớp "[Name]" hoặc whole-word "Name".</summary>
    private static bool ReferencesTable(string definition, TableDef tbl)
    {
        if (definition.Contains($"[{tbl.Name}]", StringComparison.OrdinalIgnoreCase))
            return true;
        // whole-word match (tránh khớp substring của tên khác)
        var pattern = $@"(?<![\w@#$]){System.Text.RegularExpressions.Regex.Escape(tbl.Name)}(?![\w@#$])";
        return System.Text.RegularExpressions.Regex.IsMatch(definition, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// True nếu kiểu mới (newCol) HẸP HƠN kiểu cũ (oldCol) → có thể mất data:
    /// - char/binary length giảm (-1 = MAX là rộng nhất);
    /// - precision/scale của decimal/numeric giảm;
    /// - đổi sang nhóm kiểu số nhỏ hơn.
    /// </summary>
    private static bool IsNarrowing(ColumnDef oldCol, ColumnDef newCol)
    {
        if (oldCol.IsComputed || newCol.IsComputed) return false;

        var oldType = oldCol.DataType.ToLowerInvariant();
        var newType = newCol.DataType.ToLowerInvariant();

        // Cùng kiểu chuỗi/binary → so length (chuẩn hóa MAX = int.MaxValue)
        if (oldType == newType && IsLengthType(oldType))
        {
            var oldLen = NormalizedLength(oldCol);
            var newLen = NormalizedLength(newCol);
            return newLen < oldLen;
        }

        // Cùng decimal/numeric → so precision/scale
        if (oldType == newType && (oldType is "decimal" or "numeric"))
        {
            var oldP = oldCol.NumericPrecision ?? 0;
            var newP = newCol.NumericPrecision ?? 0;
            var oldS = oldCol.NumericScale ?? 0;
            var newS = newCol.NumericScale ?? 0;
            return newP < oldP || newS < oldS;
        }

        // Đổi nhóm kiểu số → rank nhỏ hơn = hẹp hơn
        var oldRank = NumericRank(oldType);
        var newRank = NumericRank(newType);
        if (oldRank > 0 && newRank > 0)
            return newRank < oldRank;

        return false;
    }

    private static bool IsLengthType(string t) =>
        t is "varchar" or "nvarchar" or "char" or "nchar" or "varbinary" or "binary";

    /// <summary>Length chuẩn hóa: MAX (-1) coi là rộng nhất (int.MaxValue); nvarchar/nchar chia 2 sang ký tự.</summary>
    private static int NormalizedLength(ColumnDef col)
    {
        var len = col.MaxLength.GetValueOrDefault();
        if (len == -1) return int.MaxValue; // MAX
        var t = col.DataType.ToLowerInvariant();
        if (t is "nvarchar" or "nchar" && len > 0) len /= 2;
        return len;
    }

    /// <summary>Rank độ rộng nhóm kiểu số nguyên/thực (lớn hơn = rộng hơn). 0 nếu không phải kiểu số xếp hạng.</summary>
    private static int NumericRank(string t) => t switch
    {
        "tinyint" => 1,
        "smallint" => 2,
        "int" => 3,
        "bigint" => 4,
        "smallmoney" => 5,
        "money" => 6,
        "real" => 7,
        "float" => 8,
        _ => 0,
    };

    // ====================== COLUMN HELPERS ======================

    /// <summary>Định nghĩa cột đầy đủ cho CREATE / ADD COLUMN.</summary>
    private static string ColumnDefinitionText(ColumnDef col)
    {
        if (col.IsComputed)
            return $"{B(col.Name)} AS {col.ComputedDefinition}";

        var sb = new StringBuilder();
        sb.Append($"{B(col.Name)} {col.FullTypeText()}");

        if (col.IsIdentity)
        {
            // BUG 4: dùng seed/increment thật, fallback "1","1" nếu reader không có.
            var seed = string.IsNullOrEmpty(col.IdentitySeed) ? "1" : col.IdentitySeed;
            var increment = string.IsNullOrEmpty(col.IdentityIncrement) ? "1" : col.IdentityIncrement;
            sb.Append($" IDENTITY({seed},{increment})");
        }

        sb.Append(col.IsNullable ? " NULL" : " NOT NULL");

        if (!string.IsNullOrWhiteSpace(col.DefaultDefinition))
            sb.Append($" DEFAULT {col.DefaultDefinition}");

        return sb.ToString();
    }

    /// <summary>Phần định nghĩa cột cho ALTER COLUMN (không kèm identity/default).</summary>
    private static string AlterColumnTypeText(ColumnDef col)
    {
        var nullText = col.IsNullable ? "NULL" : "NOT NULL";
        return $"{B(col.Name)} {col.FullTypeText()} {nullText}";
    }

    /// <summary>Từ khóa DROP tương ứng từng loại object.</summary>
    private static string DropKeyword(DbObjectType type) => type switch
    {
        DbObjectType.View => "VIEW",
        DbObjectType.StoredProcedure => "PROCEDURE",
        DbObjectType.Function => "FUNCTION",
        DbObjectType.Trigger => "TRIGGER",
        DbObjectType.Table => "TABLE",
        DbObjectType.Sequence => "SEQUENCE",
        DbObjectType.Synonym => "SYNONYM",
        DbObjectType.Schema => "SCHEMA",
        DbObjectType.User => "USER",
        DbObjectType.Role => "ROLE",
        DbObjectType.PartitionFunction => "PARTITION FUNCTION",
        DbObjectType.PartitionScheme => "PARTITION SCHEME",
        // Permission không phải object có DROP keyword (dùng REVOKE) → fallback an toàn
        _ => "OBJECT",
    };

    /// <summary>Normalize definition (gộp whitespace) — dùng so check constraint definition.</summary>
    private static string Normalize(string? definition)
    {
        if (string.IsNullOrWhiteSpace(definition)) return "";
        var sb = new StringBuilder(definition.Length);
        var prevSpace = false;
        foreach (var ch in definition)
        {
            if (ch == '\r') continue;
            if (char.IsWhiteSpace(ch))
            {
                if (!prevSpace) { sb.Append(' '); prevSpace = true; }
            }
            else { sb.Append(ch); prevSpace = false; }
        }
        return sb.ToString().Trim();
    }

    // ====================== FORMAT HELPERS ======================

    private static void AppendHeader(StringBuilder sb, string title)
    {
        sb.AppendLine("-- ============================================================");
        sb.AppendLine($"-- {title}");
        sb.AppendLine("-- ============================================================");
    }

    /// <summary>In header 1 lần duy nhất (lazy) cho các nhóm có thể rỗng.</summary>
    private static void EnsureHeader(StringBuilder sb, ref bool emitted, string title)
    {
        if (emitted) return;
        AppendHeader(sb, title);
        emitted = true;
    }

    /// <summary>Ngăn cách giữa các bước. Khi không trong transaction → dùng GO để chia batch.</summary>
    private static void AppendSeparator(StringBuilder sb, bool inTx)
    {
        if (!inTx)
            sb.AppendLine("GO");
        sb.AppendLine();
    }
}
