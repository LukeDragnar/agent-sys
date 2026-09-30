using System.Text;
using System.Text.RegularExpressions;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Schema;

/// <summary>
/// Tùy chọn filter object + ignore rules khi so sánh schema.
/// Filter: lọc object nào được đưa vào kết quả. Ignore: bỏ qua phần nào khi so "giống/khác".
/// </summary>
public sealed class SchemaCompareOptions
{
    // ----- FILTER OBJECT -----
    /// <summary>Chỉ so các loại object trong tập này. null = mọi loại.</summary>
    public HashSet<DbObjectType>? IncludeTypes = null;

    /// <summary>Wildcard "dbo.*","dbo.Customer" so trên "schema.name". Rỗng = nhận tất cả.</summary>
    public List<string> IncludeNamePatterns = new();

    /// <summary>Wildcard loại trừ, vd "Audit.*". Match → bỏ object.</summary>
    public List<string> ExcludeNamePatterns = new();

    // ----- IGNORE RULES -----
    /// <summary>Programmable: gộp whitespace khi so (mặc định bật).</summary>
    public bool IgnoreWhiteSpace = true;

    /// <summary>Bỏ comment -- và /* */ khi so programmable.</summary>
    public bool IgnoreComments = false;

    /// <summary>Bỏ qua tên constraint (FK/check/index) khi so.</summary>
    public bool IgnoreConstraintNames = false;

    /// <summary>Bỏ COLLATE xxx khỏi signature cột khi so.</summary>
    public bool IgnoreCollation = false;

    /// <summary>Bỏ phần seed/increment của identity khi so cột.</summary>
    public bool IgnoreIdentitySeed = false;

    /// <summary>Bỏ fillfactor của index khi so (mặc định bật).</summary>
    public bool IgnoreFillFactor = true;
}

/// <summary>So sánh 2 DatabaseSchema (source = left, target = right), trả về danh sách diff item.</summary>
public sealed class SchemaComparer
{
    /// <summary>Overload cũ (giữ tương thích): dùng options mặc định.</summary>
    public SchemaComparisonResult Compare(DatabaseSchema source, DatabaseSchema target)
        => Compare(source, target, new SchemaCompareOptions());

    /// <summary>Overload mới: so sánh theo options (filter + ignore rules).</summary>
    public SchemaComparisonResult Compare(DatabaseSchema source, DatabaseSchema target, SchemaCompareOptions options)
    {
        options ??= new SchemaCompareOptions();
        var result = new SchemaComparisonResult();

        CompareTables(source, target, result, options);
        CompareSequences(source, target, result, options);
        CompareProgrammables(source, target, result, options);
        CompareSynonyms(source, target, result, options);
        CompareSchemas(source, target, result, options);

        // SECURITY
        CompareRoles(source, target, result, options);
        CompareUsers(source, target, result, options);
        ComparePermissions(source, target, result, options);

        // PARTITION
        ComparePartitionFunctions(source, target, result, options);
        ComparePartitionSchemes(source, target, result, options);

        return result;
    }

    // ====================== FILTER ======================

    /// <summary>True nếu object (theo type + "schema.name") được đưa vào kết quả theo filter.</summary>
    private static bool PassFilter(DbObjectType type, string key, SchemaCompareOptions options)
    {
        // Lọc theo loại
        if (options.IncludeTypes != null && !options.IncludeTypes.Contains(type))
            return false;

        // Loại trừ: match bất kỳ Exclude pattern → bỏ
        if (options.ExcludeNamePatterns.Any(p => WildcardMatch(p, key)))
            return false;

        // Include: nếu có khai báo pattern thì phải match ít nhất 1; rỗng = nhận tất cả
        if (options.IncludeNamePatterns.Count > 0 &&
            !options.IncludeNamePatterns.Any(p => WildcardMatch(p, key)))
            return false;

        return true;
    }

    /// <summary>So wildcard '*' (chuyển sang regex .*), IgnoreCase, full match.</summary>
    private static bool WildcardMatch(string pattern, string value)
    {
        if (string.IsNullOrEmpty(pattern)) return false;
        var rx = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(value ?? "", rx, RegexOptions.IgnoreCase);
    }

    /// <summary>So sánh toàn bộ tables. Union key của 2 bên.</summary>
    private static void CompareTables(DatabaseSchema source, DatabaseSchema target, SchemaComparisonResult result, SchemaCompareOptions options)
    {
        var allKeys = new HashSet<string>(source.Tables.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(target.Tables.Keys);

        foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            if (!PassFilter(DbObjectType.Table, key, options)) continue;

            var hasSrc = source.Tables.TryGetValue(key, out var src);
            var hasTgt = target.Tables.TryGetValue(key, out var tgt);

            var def = (hasSrc ? src : tgt)!;
            var item = new SchemaDiffItem
            {
                ObjectType = DbObjectType.Table,
                Schema = def.Schema,
                Name = def.Name,
            };

            if (hasSrc && !hasTgt)
            {
                item.Status = DiffStatus.OnlyInSource;
            }
            else if (!hasSrc && hasTgt)
            {
                item.Status = DiffStatus.OnlyInTarget;
            }
            else
            {
                var details = DiffTable(src!, tgt!, options);
                item.Status = details.Count == 0 ? DiffStatus.Identical : DiffStatus.Different;
                item.Details = details;
            }

            result.Items.Add(item);
        }
    }

    /// <summary>So chi tiết 2 table cùng tên: cột thêm/bớt/đổi kiểu + index thêm/bớt.</summary>
    private static List<string> DiffTable(TableDef src, TableDef tgt, SchemaCompareOptions options)
    {
        var details = new List<string>();

        var srcCols = src.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var tgtCols = tgt.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        // Cột chỉ có ở source → target thiếu (added so với target)
        foreach (var c in src.Columns)
        {
            if (!tgtCols.ContainsKey(c.Name))
                details.Add($"Column {c.Name}: added ({c.FullTypeText()})");
        }

        // Cột chỉ có ở target → thừa so với source (removed)
        foreach (var c in tgt.Columns)
        {
            if (!srcCols.ContainsKey(c.Name))
                details.Add($"Column {c.Name}: removed");
        }

        // Cột có cả 2 bên → so signature (áp dụng ignore collation/identity seed)
        foreach (var c in src.Columns)
        {
            if (tgtCols.TryGetValue(c.Name, out var tc) && ColumnSig(c, options) != ColumnSig(tc, options))
                details.Add($"Column {c.Name}: changed ({tc.FullTypeText()} -> {c.FullTypeText()})");
        }

        // Index: so theo Signature (áp dụng ignore constraint name/fillfactor), map theo tên
        var srcIdx = src.Indexes.ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
        var tgtIdx = tgt.Indexes.ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var i in src.Indexes)
        {
            if (!tgtIdx.ContainsKey(i.Name))
                details.Add($"Index {i.Name}: added");
            else if (IndexSig(i, options) != IndexSig(tgtIdx[i.Name], options))
                details.Add($"Index {i.Name}: changed");
        }

        foreach (var i in tgt.Indexes)
        {
            if (!srcIdx.ContainsKey(i.Name))
                details.Add($"Index {i.Name}: removed");
        }

        // Foreign key: so theo Signature, map theo tên (hoặc theo signature nếu IgnoreConstraintNames)
        DiffForeignKeys(src, tgt, options, details);

        // Check constraint: so theo definition đã normalize, map theo tên
        var srcChk = src.CheckConstraints.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var tgtChk = tgt.CheckConstraints.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        if (options.IgnoreConstraintNames)
        {
            // So theo definition (bỏ tên): tập definition 2 bên
            var srcDefs = src.CheckConstraints.Select(c => Normalize(c.Definition, options)).OrderBy(x => x).ToList();
            var tgtDefs = tgt.CheckConstraints.Select(c => Normalize(c.Definition, options)).OrderBy(x => x).ToList();
            if (!srcDefs.SequenceEqual(tgtDefs))
                details.Add("Check constraints: changed (so theo definition, bỏ tên)");
        }
        else
        {
            foreach (var c in src.CheckConstraints)
            {
                if (!tgtChk.TryGetValue(c.Name, out var tc))
                    details.Add($"Check {c.Name}: added");
                else if (Normalize(c.Definition, options) != Normalize(tc.Definition, options))
                    details.Add($"Check {c.Name}: changed");
            }

            foreach (var c in tgt.CheckConstraints)
            {
                if (!srcChk.ContainsKey(c.Name))
                    details.Add($"Check {c.Name}: removed");
            }
        }

        // Extended property: so theo Key (level + column + name), map theo Key
        var srcExt = src.ExtendedProperties.GroupBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var tgtExt = tgt.ExtendedProperties.GroupBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var e in srcExt.Values)
        {
            var label = ExtLabel(e);
            if (!tgtExt.TryGetValue(e.Key, out var te))
                details.Add($"Extended property {label}: added");
            else if (e.Value != te.Value)
                details.Add($"Extended property {label}: changed");
        }

        foreach (var e in tgtExt.Values)
        {
            if (!srcExt.ContainsKey(e.Key))
                details.Add($"Extended property {ExtLabel(e)}: removed");
        }

        return details;
    }

    /// <summary>So FK: map theo tên (mặc định) hoặc theo signature (nếu IgnoreConstraintNames).</summary>
    private static void DiffForeignKeys(TableDef src, TableDef tgt, SchemaCompareOptions options, List<string> details)
    {
        if (options.IgnoreConstraintNames)
        {
            // So theo signature (bỏ tên FK): tập signature 2 bên
            var srcSigs = src.ForeignKeys.Select(f => f.Signature()).OrderBy(x => x).ToList();
            var tgtSigs = tgt.ForeignKeys.Select(f => f.Signature()).OrderBy(x => x).ToList();
            if (!srcSigs.SequenceEqual(tgtSigs))
                details.Add("Foreign keys: changed (so theo signature, bỏ tên)");
            return;
        }

        var srcFk = src.ForeignKeys.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
        var tgtFk = tgt.ForeignKeys.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var f in src.ForeignKeys)
        {
            if (!tgtFk.ContainsKey(f.Name))
                details.Add($"Foreign key {f.Name}: added");
            else if (f.Signature() != tgtFk[f.Name].Signature())
                details.Add($"Foreign key {f.Name}: changed");
        }

        foreach (var f in tgt.ForeignKeys)
        {
            if (!srcFk.ContainsKey(f.Name))
                details.Add($"Foreign key {f.Name}: removed");
        }
    }

    /// <summary>Signature cột áp dụng ignore rules (collation, identity seed).</summary>
    private static string ColumnSig(ColumnDef col, SchemaCompareOptions options)
    {
        var sig = col.Signature();
        if (options.IgnoreCollation)
            sig = StripCollate(sig);
        if (options.IgnoreIdentitySeed)
            // Signature dùng cờ "IDENTITY" (không kèm seed/increment), nhưng strip phòng trường hợp có chứa
            sig = Regex.Replace(sig, @"IDENTITY\s*\(\s*\d+\s*,\s*\d+\s*\)", "IDENTITY", RegexOptions.IgnoreCase);
        return sig;
    }

    /// <summary>Signature index áp dụng ignore rules (constraint name, fillfactor).</summary>
    private static string IndexSig(IndexDef idx, SchemaCompareOptions options)
    {
        var sig = idx.Signature();
        if (options.IgnoreFillFactor)
            // Bỏ phần fillfactor nếu signature có lưu (vd "FILL=80")
            sig = Regex.Replace(sig, @"\|FILL=\d+", "", RegexOptions.IgnoreCase);
        // IgnoreConstraintNames cho index: signature vốn KHÔNG chứa tên index nên không cần strip thêm.
        return sig;
    }

    /// <summary>Bỏ "COLLATE xxx" khỏi 1 chuỗi (signature cột).</summary>
    private static string StripCollate(string sig) =>
        Regex.Replace(sig, @"\s*COLLATE\s+\w+", "", RegexOptions.IgnoreCase);

    /// <summary>Nhãn mô tả 1 extended property: tên + vị trí (table hoặc column).</summary>
    private static string ExtLabel(ExtendedPropertyDef e) =>
        e.Level == "COLUMN" ? $"{e.Name} on column [{e.ColumnName}]" : $"{e.Name} on table";

    /// <summary>So sánh toàn bộ sequences. Union key của 2 bên, so theo Signature.</summary>
    private static void CompareSequences(DatabaseSchema source, DatabaseSchema target, SchemaComparisonResult result, SchemaCompareOptions options)
    {
        var allKeys = new HashSet<string>(source.Sequences.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(target.Sequences.Keys);

        foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            if (!PassFilter(DbObjectType.Sequence, key, options)) continue;

            var hasSrc = source.Sequences.TryGetValue(key, out var src);
            var hasTgt = target.Sequences.TryGetValue(key, out var tgt);

            var def = (hasSrc ? src : tgt)!;
            var item = new SchemaDiffItem
            {
                ObjectType = DbObjectType.Sequence,
                Schema = def.Schema,
                Name = def.Name,
            };

            if (hasSrc && !hasTgt)
            {
                item.Status = DiffStatus.OnlyInSource;
            }
            else if (!hasSrc && hasTgt)
            {
                item.Status = DiffStatus.OnlyInTarget;
            }
            else if (src!.Signature() != tgt!.Signature())
            {
                item.Status = DiffStatus.Different;
                item.Details = DiffSequence(src, tgt);
            }
            else
            {
                item.Status = DiffStatus.Identical;
            }

            result.Items.Add(item);
        }
    }

    /// <summary>Liệt kê thuộc tính khác biệt giữa 2 sequence cùng tên.</summary>
    private static List<string> DiffSequence(SequenceDef src, SequenceDef tgt)
    {
        var details = new List<string>();
        if (!string.Equals(src.DataType, tgt.DataType, StringComparison.OrdinalIgnoreCase))
            details.Add($"Data type changed ({tgt.DataType} -> {src.DataType})");
        if (src.StartValue != tgt.StartValue)
            details.Add($"Start value changed ({tgt.StartValue} -> {src.StartValue})");
        if (src.Increment != tgt.Increment)
            details.Add($"Increment changed ({tgt.Increment} -> {src.Increment})");
        if (src.MinValue != tgt.MinValue)
            details.Add($"Min value changed ({tgt.MinValue} -> {src.MinValue})");
        if (src.MaxValue != tgt.MaxValue)
            details.Add($"Max value changed ({tgt.MaxValue} -> {src.MaxValue})");
        if (src.IsCycling != tgt.IsCycling)
            details.Add($"Cycling changed ({tgt.IsCycling} -> {src.IsCycling})");
        return details;
    }

    /// <summary>So sánh toàn bộ programmable objects bằng definition đã normalize.</summary>
    private static void CompareProgrammables(DatabaseSchema source, DatabaseSchema target, SchemaComparisonResult result, SchemaCompareOptions options)
    {
        var allKeys = new HashSet<string>(source.Programmables.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(target.Programmables.Keys);

        foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            var hasSrc = source.Programmables.TryGetValue(key, out var src);
            var hasTgt = target.Programmables.TryGetValue(key, out var tgt);

            var def = (hasSrc ? src : tgt)!;

            // Filter theo loại thực của programmable (View/SP/Function/Trigger)
            if (!PassFilter(def.ObjectType, key, options)) continue;

            var item = new SchemaDiffItem
            {
                ObjectType = def.ObjectType,
                Schema = def.Schema,
                Name = def.Name,
            };

            if (hasSrc && !hasTgt)
            {
                item.Status = DiffStatus.OnlyInSource;
            }
            else if (!hasSrc && hasTgt)
            {
                item.Status = DiffStatus.OnlyInTarget;
            }
            else if (Normalize(src!.Definition, options) != Normalize(tgt!.Definition, options))
            {
                item.Status = DiffStatus.Different;
                item.Details.Add("Definition differs");
            }
            else
            {
                item.Status = DiffStatus.Identical;
            }

            result.Items.Add(item);
        }
    }

    /// <summary>So sánh synonyms theo BaseObject. Union key của 2 bên.</summary>
    private static void CompareSynonyms(DatabaseSchema source, DatabaseSchema target, SchemaComparisonResult result, SchemaCompareOptions options)
    {
        var allKeys = new HashSet<string>(source.Synonyms.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(target.Synonyms.Keys);

        foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            if (!PassFilter(DbObjectType.Synonym, key, options)) continue;

            var hasSrc = source.Synonyms.TryGetValue(key, out var src);
            var hasTgt = target.Synonyms.TryGetValue(key, out var tgt);

            var def = (hasSrc ? src : tgt)!;
            var item = new SchemaDiffItem
            {
                ObjectType = DbObjectType.Synonym,
                Schema = def.Schema,
                Name = def.Name,
            };

            if (hasSrc && !hasTgt)
            {
                item.Status = DiffStatus.OnlyInSource;
            }
            else if (!hasSrc && hasTgt)
            {
                item.Status = DiffStatus.OnlyInTarget;
            }
            else if (!string.Equals(src!.BaseObject, tgt!.BaseObject, StringComparison.OrdinalIgnoreCase))
            {
                item.Status = DiffStatus.Different;
                item.Details.Add($"Base object changed ({tgt.BaseObject} -> {src.BaseObject})");
            }
            else
            {
                item.Status = DiffStatus.Identical;
            }

            result.Items.Add(item);
        }
    }

    /// <summary>So sánh schemas (chỉ tồn tại/không). Union tên của 2 bên.</summary>
    private static void CompareSchemas(DatabaseSchema source, DatabaseSchema target, SchemaComparisonResult result, SchemaCompareOptions options)
    {
        var allKeys = new HashSet<string>(source.Schemas.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(target.Schemas.Keys);

        foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            if (!PassFilter(DbObjectType.Schema, key, options)) continue;

            var hasSrc = source.Schemas.ContainsKey(key);
            var hasTgt = target.Schemas.ContainsKey(key);

            var item = new SchemaDiffItem
            {
                ObjectType = DbObjectType.Schema,
                Schema = key,   // schema không có "schema cha" — dùng chính tên nó
                Name = key,
            };

            // Schema chỉ so tồn tại/không (không có thuộc tính để Different)
            if (hasSrc && !hasTgt) item.Status = DiffStatus.OnlyInSource;
            else if (!hasSrc && hasTgt) item.Status = DiffStatus.OnlyInTarget;
            else item.Status = DiffStatus.Identical;

            result.Items.Add(item);
        }
    }

    // ====================== SECURITY ======================

    /// <summary>So sánh roles (chỉ tồn tại/không). Union tên của 2 bên.</summary>
    private static void CompareRoles(DatabaseSchema source, DatabaseSchema target, SchemaComparisonResult result, SchemaCompareOptions options)
    {
        var allKeys = new HashSet<string>(source.Roles.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(target.Roles.Keys);

        foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            if (!PassFilter(DbObjectType.Role, key, options)) continue;

            var hasSrc = source.Roles.ContainsKey(key);
            var hasTgt = target.Roles.ContainsKey(key);

            var item = new SchemaDiffItem
            {
                ObjectType = DbObjectType.Role,
                Schema = "",   // role không thuộc schema
                Name = key,
            };

            // Role chỉ so tồn tại/không (không có thuộc tính để Different)
            if (hasSrc && !hasTgt) item.Status = DiffStatus.OnlyInSource;
            else if (!hasSrc && hasTgt) item.Status = DiffStatus.OnlyInTarget;
            else item.Status = DiffStatus.Identical;

            result.Items.Add(item);
        }
    }

    /// <summary>So sánh users theo Signature (type + default schema + auth + role memberships).</summary>
    private static void CompareUsers(DatabaseSchema source, DatabaseSchema target, SchemaComparisonResult result, SchemaCompareOptions options)
    {
        var allKeys = new HashSet<string>(source.Users.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(target.Users.Keys);

        foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            if (!PassFilter(DbObjectType.User, key, options)) continue;

            var hasSrc = source.Users.TryGetValue(key, out var src);
            var hasTgt = target.Users.TryGetValue(key, out var tgt);

            var item = new SchemaDiffItem
            {
                ObjectType = DbObjectType.User,
                Schema = "",
                Name = key,
            };

            if (hasSrc && !hasTgt)
            {
                item.Status = DiffStatus.OnlyInSource;
            }
            else if (!hasSrc && hasTgt)
            {
                item.Status = DiffStatus.OnlyInTarget;
            }
            else if (src!.Signature() != tgt!.Signature())
            {
                item.Status = DiffStatus.Different;
                item.Details = DiffUser(src, tgt);
            }
            else
            {
                item.Status = DiffStatus.Identical;
            }

            result.Items.Add(item);
        }
    }

    /// <summary>Liệt kê khác biệt giữa 2 user cùng tên.</summary>
    private static List<string> DiffUser(DbUserDef src, DbUserDef tgt)
    {
        var details = new List<string>();
        if (!string.Equals(src.TypeDesc, tgt.TypeDesc, StringComparison.OrdinalIgnoreCase))
            details.Add($"Type changed ({tgt.TypeDesc} -> {src.TypeDesc})");
        if (!string.Equals(src.DefaultSchema, tgt.DefaultSchema, StringComparison.OrdinalIgnoreCase))
            details.Add($"Default schema changed ({tgt.DefaultSchema} -> {src.DefaultSchema})");
        if (!string.Equals(src.AuthenticationDesc, tgt.AuthenticationDesc, StringComparison.OrdinalIgnoreCase))
            details.Add($"Authentication changed ({tgt.AuthenticationDesc} -> {src.AuthenticationDesc})");

        // Role memberships: thêm/bớt
        var srcRoles = new HashSet<string>(src.Roles, StringComparer.OrdinalIgnoreCase);
        var tgtRoles = new HashSet<string>(tgt.Roles, StringComparer.OrdinalIgnoreCase);
        foreach (var r in src.Roles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            if (!tgtRoles.Contains(r)) details.Add($"Role membership [{r}]: added");
        foreach (var r in tgt.Roles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            if (!srcRoles.Contains(r)) details.Add($"Role membership [{r}]: removed");

        return details;
    }

    /// <summary>So sánh permissions theo Key (set compare). Key gồm hết → chỉ tồn tại/không (không Different).</summary>
    private static void ComparePermissions(DatabaseSchema source, DatabaseSchema target, SchemaComparisonResult result, SchemaCompareOptions options)
    {
        var srcMap = source.Permissions.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var tgtMap = target.Permissions.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var allKeys = new HashSet<string>(srcMap.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(tgtMap.Keys);

        foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            // Filter theo "schema.name" — permission dùng Grantee làm name để filter
            var def = (srcMap.TryGetValue(key, out var s) ? s : tgtMap[key]);
            if (!PassFilter(DbObjectType.Permission, def.Grantee, options)) continue;

            var hasSrc = srcMap.ContainsKey(key);
            var hasTgt = tgtMap.ContainsKey(key);

            var item = new SchemaDiffItem
            {
                ObjectType = DbObjectType.Permission,
                Schema = "",
                // Name mô tả ngắn permission; "$"+Key đảm bảo duy nhất
                Name = "$" + def.Key,
            };
            item.Details.Add(PermissionLabel(def));

            // Permission không có "Different" (Key gồm hết) → chỉ OnlyInSource/OnlyInTarget/Identical
            if (hasSrc && !hasTgt) item.Status = DiffStatus.OnlyInSource;
            else if (!hasSrc && hasTgt) item.Status = DiffStatus.OnlyInTarget;
            else item.Status = DiffStatus.Identical;

            result.Items.Add(item);
        }
    }

    /// <summary>Nhãn mô tả 1 permission: "GRANT SELECT ON [s].[o] TO [u]".</summary>
    private static string PermissionLabel(DbPermissionDef p)
    {
        var on = string.IsNullOrEmpty(p.TargetName) ? "" : $" ON {p.TargetName}";
        return $"{p.StateDesc} {p.PermissionName}{on} TO [{p.Grantee}]";
    }

    // ====================== PARTITION ======================

    /// <summary>So sánh partition functions theo Signature. Union key của 2 bên.</summary>
    private static void ComparePartitionFunctions(DatabaseSchema source, DatabaseSchema target, SchemaComparisonResult result, SchemaCompareOptions options)
    {
        var allKeys = new HashSet<string>(source.PartitionFunctions.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(target.PartitionFunctions.Keys);

        foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            if (!PassFilter(DbObjectType.PartitionFunction, key, options)) continue;

            var hasSrc = source.PartitionFunctions.TryGetValue(key, out var src);
            var hasTgt = target.PartitionFunctions.TryGetValue(key, out var tgt);

            var item = new SchemaDiffItem
            {
                ObjectType = DbObjectType.PartitionFunction,
                Schema = "",
                Name = key,
            };

            if (hasSrc && !hasTgt)
            {
                item.Status = DiffStatus.OnlyInSource;
            }
            else if (!hasSrc && hasTgt)
            {
                item.Status = DiffStatus.OnlyInTarget;
            }
            else if (src!.Signature() != tgt!.Signature())
            {
                item.Status = DiffStatus.Different;
                if (!string.Equals(src.ParameterType, tgt.ParameterType, StringComparison.OrdinalIgnoreCase))
                    item.Details.Add($"Parameter type changed ({tgt.ParameterType} -> {src.ParameterType})");
                if (src.RangeRight != tgt.RangeRight)
                    item.Details.Add($"Range direction changed ({(tgt.RangeRight ? "RIGHT" : "LEFT")} -> {(src.RangeRight ? "RIGHT" : "LEFT")})");
                if (!src.BoundaryValues.SequenceEqual(tgt.BoundaryValues))
                    item.Details.Add($"Boundary values changed ([{string.Join(",", tgt.BoundaryValues)}] -> [{string.Join(",", src.BoundaryValues)}])");
            }
            else
            {
                item.Status = DiffStatus.Identical;
            }

            result.Items.Add(item);
        }
    }

    /// <summary>So sánh partition schemes theo Signature. Union key của 2 bên.</summary>
    private static void ComparePartitionSchemes(DatabaseSchema source, DatabaseSchema target, SchemaComparisonResult result, SchemaCompareOptions options)
    {
        var allKeys = new HashSet<string>(source.PartitionSchemes.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(target.PartitionSchemes.Keys);

        foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            if (!PassFilter(DbObjectType.PartitionScheme, key, options)) continue;

            var hasSrc = source.PartitionSchemes.TryGetValue(key, out var src);
            var hasTgt = target.PartitionSchemes.TryGetValue(key, out var tgt);

            var item = new SchemaDiffItem
            {
                ObjectType = DbObjectType.PartitionScheme,
                Schema = "",
                Name = key,
            };

            if (hasSrc && !hasTgt)
            {
                item.Status = DiffStatus.OnlyInSource;
            }
            else if (!hasSrc && hasTgt)
            {
                item.Status = DiffStatus.OnlyInTarget;
            }
            else if (src!.Signature() != tgt!.Signature())
            {
                item.Status = DiffStatus.Different;
                if (!string.Equals(src.FunctionName, tgt.FunctionName, StringComparison.OrdinalIgnoreCase))
                    item.Details.Add($"Function changed ({tgt.FunctionName} -> {src.FunctionName})");
                if (!src.Filegroups.SequenceEqual(tgt.Filegroups))
                    item.Details.Add($"Filegroups changed ([{string.Join(",", tgt.Filegroups)}] -> [{string.Join(",", src.Filegroups)}])");
            }
            else
            {
                item.Status = DiffStatus.Identical;
            }

            result.Items.Add(item);
        }
    }

    /// <summary>
    /// Normalize definition theo options: bỏ CR, (tùy chọn) bỏ comment, gộp whitespace, trim.
    /// IgnoreWhiteSpace = false → giữ nguyên whitespace (chỉ bỏ CR + trim).
    /// </summary>
    private static string Normalize(string? definition, SchemaCompareOptions options)
    {
        if (string.IsNullOrWhiteSpace(definition)) return "";

        var text = definition;

        // Bỏ comment trước (để whitespace của comment cũng biến mất khi gộp)
        if (options.IgnoreComments)
            text = StripComments(text);

        if (!options.IgnoreWhiteSpace)
        {
            // Giữ whitespace nội bộ, chỉ bỏ CR + trim
            return text.Replace("\r", "").Trim();
        }

        var sb = new StringBuilder(text.Length);
        var prevSpace = false;
        foreach (var ch in text)
        {
            if (ch == '\r') continue;
            if (char.IsWhiteSpace(ch))
            {
                if (!prevSpace) { sb.Append(' '); prevSpace = true; }
            }
            else
            {
                sb.Append(ch);
                prevSpace = false;
            }
        }
        return sb.ToString().Trim();
    }

    /// <summary>Bỏ comment dòng (-- ... đến hết dòng) và comment khối (/* ... */).</summary>
    private static string StripComments(string text)
    {
        // Bỏ block comment /* ... */ (đa dòng)
        text = Regex.Replace(text, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        // Bỏ line comment -- ... đến hết dòng
        text = Regex.Replace(text, @"--[^\r\n]*", " ");
        return text;
    }
}
