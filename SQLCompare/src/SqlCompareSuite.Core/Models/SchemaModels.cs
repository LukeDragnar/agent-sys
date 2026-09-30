namespace SqlCompareSuite.Core.Models;

/// <summary>Loại object trong database.</summary>
public enum DbObjectType
{
    Table,
    View,
    StoredProcedure,
    Function,
    Trigger,
    Sequence,   // thêm ở CUỐI để không vỡ giá trị cũ
    Synonym,    // thêm ở CUỐI
    Schema,     // thêm ở CUỐI
    User,               // SECURITY — database user
    Role,               // SECURITY — database role
    Permission,         // SECURITY — grant/deny permission
    PartitionFunction,  // PARTITION — partition function
    PartitionScheme,    // PARTITION — partition scheme
}

/// <summary>1 cột của table.</summary>
public sealed class ColumnDef
{
    public string Name { get; set; } = "";
    public int OrdinalPosition { get; set; }
    public string DataType { get; set; } = "";
    public int? MaxLength { get; set; }          // char/varchar/varbinary length (-1 = MAX)
    public int? NumericPrecision { get; set; }
    public int? NumericScale { get; set; }
    public bool IsNullable { get; set; }
    public string? DefaultDefinition { get; set; }
    public bool IsIdentity { get; set; }
    public string? IdentitySeed;        // seed_value của identity (null nếu không phải identity)
    public string? IdentityIncrement;   // increment_value của identity (null nếu không phải identity)
    public bool IsComputed { get; set; }
    public string? ComputedDefinition { get; set; }

    /// <summary>Sinh chuỗi kiểu dữ liệu đầy đủ: nvarchar(50), decimal(18,2), int...</summary>
    public string FullTypeText()
    {
        var t = DataType.ToLowerInvariant();
        switch (t)
        {
            case "varchar":
            case "nvarchar":
            case "char":
            case "nchar":
            case "varbinary":
            case "binary":
                var len = MaxLength.GetValueOrDefault();
                // nvarchar/nchar lưu max_length theo byte → chia 2 cho số ký tự
                if (t is "nvarchar" or "nchar" && len > 0) len /= 2;
                return len == -1 ? $"{t}(max)" : $"{t}({len})";
            case "decimal":
            case "numeric":
                return $"{t}({NumericPrecision},{NumericScale})";
            case "datetime2":
            case "datetimeoffset":
            case "time":
                return NumericScale.HasValue ? $"{t}({NumericScale})" : t;
            default:
                return t;
        }
    }

    /// <summary>Chuỗi chuẩn hóa để so sánh 2 cột có "giống nhau" không.</summary>
    public string Signature()
    {
        if (IsComputed)
            return $"COMPUTED({ComputedDefinition?.Trim()})";
        return $"{FullTypeText()}|{(IsNullable ? "NULL" : "NOTNULL")}|{(IsIdentity ? "IDENTITY" : "")}|DEF={DefaultDefinition?.Trim()}";
    }
}

/// <summary>1 index của table.</summary>
public sealed class IndexDef
{
    public string Name { get; set; } = "";
    public bool IsPrimaryKey { get; set; }
    public bool IsUnique { get; set; }
    public bool IsClustered { get; set; }
    public List<string> Columns { get; set; } = new();

    /// <summary>Cột INCLUDE(...) của nonclustered index (is_included_column = 1), theo thứ tự.</summary>
    public List<string> IncludedColumns { get; set; } = new();

    /// <summary>Định nghĩa filter của filtered index (sys.indexes.filter_definition), null nếu không có.</summary>
    public string? FilterDefinition { get; set; }

    public string Signature() =>
        $"{(IsPrimaryKey ? "PK" : "")}|{(IsUnique ? "UQ" : "")}|{(IsClustered ? "CL" : "NCL")}|{string.Join(",", Columns)}|INC={string.Join(",", IncludedColumns)}|FLT={FilterDefinition?.Trim()}";
}

/// <summary>1 foreign key của table.</summary>
public sealed class ForeignKeyDef
{
    public string Name { get; set; } = "";
    public List<string> Columns { get; set; } = new();      // cột (FK) ở bảng hiện tại
    public string RefSchema { get; set; } = "dbo";
    public string RefTable { get; set; } = "";
    public List<string> RefColumns { get; set; } = new();   // cột được tham chiếu ở bảng đích
    public string OnDeleteAction { get; set; } = "NO_ACTION";   // NO_ACTION/CASCADE/SET_NULL/SET_DEFAULT
    public string OnUpdateAction { get; set; } = "NO_ACTION";

    /// <summary>Chuỗi chuẩn hóa để so sánh 2 FK có "giống nhau" không (bỏ qua tên FK).</summary>
    public string Signature() =>
        $"{string.Join(",", Columns)}->[{RefSchema}].[{RefTable}]({string.Join(",", RefColumns)})|DEL={OnDeleteAction}|UPD={OnUpdateAction}";
}

/// <summary>1 check constraint của table.</summary>
public sealed class CheckConstraintDef
{
    public string Name { get; set; } = "";
    public string Definition { get; set; } = "";
}

/// <summary>1 extended property (vd MS_Description) ở cấp table hoặc column.</summary>
public sealed class ExtendedPropertyDef
{
    public string Level { get; set; } = "TABLE";   // "TABLE" hoặc "COLUMN"
    public string TableSchema { get; set; } = "dbo";
    public string TableName { get; set; } = "";
    public string? ColumnName { get; set; }         // null nếu cấp table
    public string Name { get; set; } = "MS_Description";
    public string Value { get; set; } = "";

    /// <summary>Khóa định danh property trong phạm vi 1 bảng (level + column + name).</summary>
    public string Key => $"{Level}|{ColumnName ?? ""}|{Name}";

    /// <summary>Chuỗi chuẩn hóa để so sánh.</summary>
    public string Signature() => $"{Key}={Value}";
}

/// <summary>1 sequence object (sys.sequences).</summary>
public sealed class SequenceDef
{
    public string Schema { get; set; } = "dbo";
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";      // vd "bigint"
    public string StartValue { get; set; } = "";
    public string Increment { get; set; } = "";
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public bool IsCycling { get; set; }

    public string FullName => $"[{Schema}].[{Name}]";
    public string Key => $"{Schema}.{Name}";

    /// <summary>Chuỗi chuẩn hóa để so sánh 2 sequence có "giống nhau" không.</summary>
    public string Signature() =>
        $"TYPE={DataType}|START={StartValue}|INC={Increment}|MIN={MinValue}|MAX={MaxValue}|CYCLE={(IsCycling ? "1" : "0")}";
}

/// <summary>Định nghĩa 1 table: columns + indexes + PK + FK + check constraints.</summary>
public sealed class TableDef
{
    public string Schema { get; set; } = "dbo";
    public string Name { get; set; } = "";
    public List<ColumnDef> Columns { get; set; } = new();
    public List<IndexDef> Indexes { get; set; } = new();
    public List<ForeignKeyDef> ForeignKeys { get; set; } = new();
    public List<CheckConstraintDef> CheckConstraints { get; set; } = new();
    public List<ExtendedPropertyDef> ExtendedProperties { get; set; } = new();

    public string FullName => $"[{Schema}].[{Name}]";
    public string Key => $"{Schema}.{Name}";

    public IndexDef? PrimaryKey => Indexes.FirstOrDefault(i => i.IsPrimaryKey);

    /// <summary>Chuỗi chuẩn hóa toàn bộ table (cột + index + FK + check) để so sánh nhanh.</summary>
    public string Signature()
    {
        var cols = string.Join(";", Columns.OrderBy(c => c.OrdinalPosition).Select(c => $"{c.Name}={c.Signature()}"));
        var idx = string.Join(";", Indexes.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).Select(i => $"{i.Name}={i.Signature()}"));
        var fks = string.Join(";", ForeignKeys.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).Select(f => $"{f.Name}={f.Signature()}"));
        var chk = string.Join(";", CheckConstraints.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).Select(c => $"{c.Name}={c.Definition}"));
        // Extended properties gộp vào cuối: bảng khác extended property bị coi là Different
        var ext = string.Join(";", ExtendedProperties.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase).Select(e => e.Signature()));
        return $"COLS[{cols}]IDX[{idx}]FK[{fks}]CHK[{chk}]EXT[{ext}]";
    }
}

/// <summary>1 synonym (sys.synonyms) — alias trỏ tới base object.</summary>
public sealed class SynonymDef
{
    public string Schema { get; set; } = "dbo";
    public string Name { get; set; } = "";
    public string BaseObject { get; set; } = "";   // base_object_name, vd [db].[schema].[obj]

    public string FullName => $"[{Schema}].[{Name}]";
    public string Key => $"{Schema}.{Name}";
}

/// <summary>1 user schema (sys.schemas) — chỉ giữ tên để CREATE/DROP SCHEMA.</summary>
public sealed class SchemaDef
{
    public string Name { get; set; } = "";
}

// ====================== SECURITY ======================

/// <summary>1 database user (sys.database_principals type S/U/G/E/X).</summary>
public sealed class DbUserDef
{
    public string Name = "";
    public string TypeDesc = "";            // SQL_USER / WINDOWS_USER / ...
    public string? DefaultSchema;
    public string AuthenticationDesc = "";  // NONE / INSTANCE / DATABASE...
    public List<string> Roles = new();      // role memberships (tên role)

    public string Key => Name;
    public string Signature() => $"{TypeDesc}|{DefaultSchema}|{AuthenticationDesc}|R={string.Join(",", Roles.OrderBy(x => x))}";
}

/// <summary>1 database role (sys.database_principals type R, is_fixed_role = 0).</summary>
public sealed class DbRoleDef
{
    public string Name = "";
    public string Key => Name;
}

/// <summary>1 permission (sys.database_permissions) — GRANT/DENY ở cấp DATABASE/SCHEMA/OBJECT.</summary>
public sealed class DbPermissionDef
{
    public string StateDesc = "";       // GRANT / DENY / GRANT_WITH_GRANT_OPTION
    public string PermissionName = "";  // SELECT / EXECUTE / ...
    public string ClassDesc = "";       // DATABASE / SCHEMA / OBJECT_OR_COLUMN / TYPE
    public string TargetName = "";      // "" cho DATABASE; "[schema]" cho SCHEMA; "[schema].[obj]" cho OBJECT
    public string Grantee = "";         // principal

    public string Key => $"{StateDesc}|{PermissionName}|{ClassDesc}|{TargetName}|{Grantee}";
}

// ====================== PARTITION ======================

/// <summary>1 partition function (sys.partition_functions).</summary>
public sealed class PartitionFunctionDef
{
    public string Name = "";
    public string ParameterType = "";       // kiểu tham số (vd int, datetime, varchar(10))
    public bool RangeRight;                  // boundary_value_on_right → RANGE RIGHT, ngược lại LEFT
    public List<string> BoundaryValues = new();

    public string Key => Name;
    public string Signature() => $"{ParameterType}|{(RangeRight ? "RIGHT" : "LEFT")}|{string.Join(",", BoundaryValues)}";
}

/// <summary>1 partition scheme (sys.partition_schemes) — map function tới các filegroup.</summary>
public sealed class PartitionSchemeDef
{
    public string Name = "";
    public string FunctionName = "";
    public List<string> Filegroups = new();

    public string Key => Name;
    public string Signature() => $"{FunctionName}|{string.Join(",", Filegroups)}";
}

/// <summary>Object dạng code (View/SP/Function/Trigger) — so sánh bằng definition text.</summary>
public sealed class ProgrammableObject
{
    public string Schema { get; set; } = "dbo";
    public string Name { get; set; } = "";
    public DbObjectType ObjectType { get; set; }
    public string Definition { get; set; } = "";

    public string FullName => $"[{Schema}].[{Name}]";
    public string Key => $"{Schema}.{Name}";
}

/// <summary>Toàn bộ schema đọc được từ 1 database.</summary>
public sealed class DatabaseSchema
{
    public Dictionary<string, TableDef> Tables { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, ProgrammableObject> Programmables { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, SequenceDef> Sequences { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, SynonymDef> Synonyms { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, SchemaDef> Schemas { get; } = new(StringComparer.OrdinalIgnoreCase);

    // SECURITY
    public Dictionary<string, DbUserDef> Users { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, DbRoleDef> Roles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<DbPermissionDef> Permissions { get; } = new();

    // PARTITION
    public Dictionary<string, PartitionFunctionDef> PartitionFunctions { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, PartitionSchemeDef> PartitionSchemes { get; } = new(StringComparer.OrdinalIgnoreCase);
}
