using Microsoft.Data.SqlClient;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Schema;

/// <summary>Đọc toàn bộ schema (tables/columns/indexes + programmable objects) từ 1 database.</summary>
public sealed class SchemaReader
{
    /// <summary>
    /// Thời gian sửa cuối (sys.objects.modify_date) của mọi object: "schema.name" -> modify_date.
    /// NOLOCK (OpenRead). Dùng cho cột "Last modified" ở grid schema. Object không nằm trong
    /// sys.objects (user/role/permission/partition) sẽ không có trong map.
    /// </summary>
    public System.Collections.Generic.Dictionary<string, System.DateTime> ReadModifyDates(DbConnectionInfo conn)
    {
        var map = new System.Collections.Generic.Dictionary<string, System.DateTime>(System.StringComparer.OrdinalIgnoreCase);
        const string sql = @"
SELECT SCHEMA_NAME(o.schema_id) AS sch, o.name AS nm, o.modify_date AS md
FROM sys.objects o
WHERE o.is_ms_shipped = 0 AND o.type IN ('U','V','P','FN','IF','TF','TR','SO');";

        using var connection = new SqlConnection(conn.BuildConnectionString());
        connection.OpenRead();
        using var cmd = new SqlCommand(sql, connection);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
            map[$"{rdr.GetString(0)}.{rdr.GetString(1)}"] = rdr.GetDateTime(2);
        return map;
    }

    public DatabaseSchema Read(DbConnectionInfo conn)
    {
        var schema = new DatabaseSchema();

        using var connection = new SqlConnection(conn.BuildConnectionString());
        connection.OpenRead();

        ReadTablesAndColumns(connection, schema);
        ReadIndexes(connection, schema);
        ReadForeignKeys(connection, schema);
        ReadCheckConstraints(connection, schema);
        ReadExtendedProperties(connection, schema);
        ReadSequences(connection, schema);
        ReadProgrammables(connection, schema);
        ReadSynonyms(connection, schema);
        ReadSchemas(connection, schema);

        // SECURITY: users + roles trước, rồi role memberships, rồi permissions
        ReadUsersAndRoles(connection, schema);
        ReadRoleMemberships(connection, schema);
        ReadPermissions(connection, schema);

        // PARTITION: function trước, scheme sau
        ReadPartitionFunctions(connection, schema);
        ReadPartitionSchemes(connection, schema);

        return schema;
    }

    /// <summary>Đọc tables + columns. Tạo TableDef cho từng table (bỏ object hệ thống).</summary>
    private static void ReadTablesAndColumns(SqlConnection connection, DatabaseSchema schema)
    {
        const string sql = @"
SELECT
    s.name                                  AS SchemaName,
    t.name                                  AS TableName,
    c.name                                  AS ColumnName,
    c.column_id                             AS OrdinalPosition,
    typ.name                                AS DataType,
    c.max_length                            AS MaxLength,
    c.precision                             AS NumericPrecision,
    c.scale                                 AS NumericScale,
    c.is_nullable                           AS IsNullable,
    c.is_identity                           AS IsIdentity,
    ic.seed_value                           AS IdentitySeed,
    ic.increment_value                      AS IdentityIncrement,
    c.is_computed                           AS IsComputed,
    dc.definition                           AS DefaultDefinition,
    cc.definition                           AS ComputedDefinition
FROM sys.tables t
INNER JOIN sys.schemas s            ON s.schema_id = t.schema_id
INNER JOIN sys.columns c            ON c.object_id = t.object_id
INNER JOIN sys.types  typ           ON typ.user_type_id = c.user_type_id
LEFT  JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
LEFT  JOIN sys.computed_columns cc  ON cc.object_id = c.object_id AND cc.column_id = c.column_id
LEFT  JOIN sys.identity_columns ic  ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.column_id;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var schemaName = reader.GetString(reader.GetOrdinal("SchemaName"));
            var tableName = reader.GetString(reader.GetOrdinal("TableName"));
            var key = $"{schemaName}.{tableName}";

            if (!schema.Tables.TryGetValue(key, out var table))
            {
                table = new TableDef { Schema = schemaName, Name = tableName };
                schema.Tables[key] = table;
            }

            var col = new ColumnDef
            {
                Name = reader.GetString(reader.GetOrdinal("ColumnName")),
                OrdinalPosition = reader.GetInt32(reader.GetOrdinal("OrdinalPosition")),
                DataType = reader.GetString(reader.GetOrdinal("DataType")),
                MaxLength = GetNullableInt16AsInt(reader, "MaxLength"),
                NumericPrecision = GetNullableByteAsInt(reader, "NumericPrecision"),
                NumericScale = GetNullableByteAsInt(reader, "NumericScale"),
                IsNullable = GetBool(reader, "IsNullable"),
                IsIdentity = GetBool(reader, "IsIdentity"),
                // seed/increment là sql_variant → đọc object rồi ToString invariant (số không bị format theo locale)
                IdentitySeed = VariantToString(GetNullableObject(reader, "IdentitySeed")),
                IdentityIncrement = VariantToString(GetNullableObject(reader, "IdentityIncrement")),
                IsComputed = GetBool(reader, "IsComputed"),
                DefaultDefinition = GetNullableString(reader, "DefaultDefinition"),
                ComputedDefinition = GetNullableString(reader, "ComputedDefinition"),
            };

            table.Columns.Add(col);
        }
    }

    /// <summary>
    /// Đọc indexes (bỏ heap type=0). Gom KEY columns theo key_ordinal + INCLUDED columns theo thứ tự.
    /// Đọc luôn filter_definition cho filtered index (has_filter = 1).
    /// </summary>
    private static void ReadIndexes(SqlConnection connection, DatabaseSchema schema)
    {
        // is_included_column + index_column_id để sort đúng: key columns trước (theo key_ordinal),
        // rồi tới included columns (theo index_column_id). filter_definition đọc kèm.
        const string sql = @"
SELECT
    s.name                  AS SchemaName,
    t.name                  AS TableName,
    i.name                  AS IndexName,
    i.is_primary_key        AS IsPrimaryKey,
    i.is_unique             AS IsUnique,
    i.type                  AS IndexType,           -- 1 = clustered
    i.has_filter            AS HasFilter,
    i.filter_definition     AS FilterDefinition,
    c.name                  AS ColumnName,
    ic.is_included_column   AS IsIncluded,
    ic.key_ordinal          AS KeyOrdinal,
    ic.index_column_id      AS IndexColumnId
FROM sys.indexes i
INNER JOIN sys.tables t         ON t.object_id = i.object_id
INNER JOIN sys.schemas s        ON s.schema_id = t.schema_id
INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE t.is_ms_shipped = 0
  AND i.type <> 0           -- bỏ heap
ORDER BY s.name, t.name, i.name, ic.is_included_column, ic.key_ordinal, ic.index_column_id;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        // Track index đang build theo key (table.index) để gom column theo thứ tự
        var indexMap = new Dictionary<string, IndexDef>(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            var schemaName = reader.GetString(reader.GetOrdinal("SchemaName"));
            var tableName = reader.GetString(reader.GetOrdinal("TableName"));
            var indexName = GetNullableString(reader, "IndexName") ?? "";
            var tableKey = $"{schemaName}.{tableName}";

            if (!schema.Tables.TryGetValue(tableKey, out var table))
                continue; // table không nằm trong danh sách đã đọc → bỏ qua

            var indexKey = $"{tableKey}.{indexName}";
            if (!indexMap.TryGetValue(indexKey, out var index))
            {
                index = new IndexDef
                {
                    Name = indexName,
                    IsPrimaryKey = GetBool(reader, "IsPrimaryKey"),
                    IsUnique = GetBool(reader, "IsUnique"),
                    IsClustered = GetByte(reader, "IndexType") == 1,
                    // chỉ lưu filter nếu index có filter
                    FilterDefinition = GetBool(reader, "HasFilter") ? GetNullableString(reader, "FilterDefinition") : null,
                };
                indexMap[indexKey] = index;
                table.Indexes.Add(index);
            }

            var columnName = GetNullableString(reader, "ColumnName");
            if (string.IsNullOrEmpty(columnName)) continue;

            // is_included_column = 1 → đưa vào IncludedColumns, ngược lại là key column
            if (GetBool(reader, "IsIncluded"))
                index.IncludedColumns.Add(columnName);
            else
                index.Columns.Add(columnName);
        }
    }

    /// <summary>Đọc foreign keys: join sys.foreign_keys + sys.foreign_key_columns + columns/tables/schemas.</summary>
    private static void ReadForeignKeys(SqlConnection connection, DatabaseSchema schema)
    {
        const string sql = @"
SELECT
    ps.name     AS SchemaName,        -- schema bảng chứa FK (parent)
    pt.name     AS TableName,         -- bảng chứa FK (parent)
    fk.name     AS FkName,
    pc.name     AS ColumnName,        -- cột FK ở parent
    rs.name     AS RefSchemaName,     -- schema bảng tham chiếu (referenced)
    rt.name     AS RefTableName,      -- bảng tham chiếu
    rc.name     AS RefColumnName,     -- cột tham chiếu
    fkc.constraint_column_id AS KeyOrdinal,
    fk.delete_referential_action_desc AS OnDeleteAction,
    fk.update_referential_action_desc AS OnUpdateAction
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.tables  pt  ON pt.object_id = fk.parent_object_id
INNER JOIN sys.schemas ps  ON ps.schema_id = pt.schema_id
INNER JOIN sys.columns pc  ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
INNER JOIN sys.tables  rt  ON rt.object_id = fk.referenced_object_id
INNER JOIN sys.schemas rs  ON rs.schema_id = rt.schema_id
INNER JOIN sys.columns rc  ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
WHERE pt.is_ms_shipped = 0
ORDER BY ps.name, pt.name, fk.name, fkc.constraint_column_id;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        // Gom cột (FK + ref) theo thứ tự key_ordinal cho từng FK
        var fkMap = new Dictionary<string, ForeignKeyDef>(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            var schemaName = reader.GetString(reader.GetOrdinal("SchemaName"));
            var tableName = reader.GetString(reader.GetOrdinal("TableName"));
            var fkName = GetNullableString(reader, "FkName") ?? "";
            var tableKey = $"{schemaName}.{tableName}";

            if (!schema.Tables.TryGetValue(tableKey, out var table))
                continue; // bảng không có trong danh sách → bỏ qua

            var fkKey = $"{tableKey}.{fkName}";
            if (!fkMap.TryGetValue(fkKey, out var fk))
            {
                fk = new ForeignKeyDef
                {
                    Name = fkName,
                    RefSchema = GetNullableString(reader, "RefSchemaName") ?? "dbo",
                    RefTable = GetNullableString(reader, "RefTableName") ?? "",
                    OnDeleteAction = GetNullableString(reader, "OnDeleteAction") ?? "NO_ACTION",
                    OnUpdateAction = GetNullableString(reader, "OnUpdateAction") ?? "NO_ACTION",
                };
                fkMap[fkKey] = fk;
                table.ForeignKeys.Add(fk);
            }

            var colName = GetNullableString(reader, "ColumnName");
            if (!string.IsNullOrEmpty(colName)) fk.Columns.Add(colName);

            var refColName = GetNullableString(reader, "RefColumnName");
            if (!string.IsNullOrEmpty(refColName)) fk.RefColumns.Add(refColName);
        }
    }

    /// <summary>Đọc check constraints từ sys.check_constraints (name + definition).</summary>
    private static void ReadCheckConstraints(SqlConnection connection, DatabaseSchema schema)
    {
        const string sql = @"
SELECT
    s.name  AS SchemaName,
    t.name  AS TableName,
    cc.name AS CheckName,
    cc.definition AS Definition
FROM sys.check_constraints cc
INNER JOIN sys.tables  t ON t.object_id = cc.parent_object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, cc.name;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var schemaName = reader.GetString(reader.GetOrdinal("SchemaName"));
            var tableName = reader.GetString(reader.GetOrdinal("TableName"));
            var tableKey = $"{schemaName}.{tableName}";

            if (!schema.Tables.TryGetValue(tableKey, out var table))
                continue;

            table.CheckConstraints.Add(new CheckConstraintDef
            {
                Name = GetNullableString(reader, "CheckName") ?? "",
                Definition = GetNullableString(reader, "Definition") ?? "",
            });
        }
    }

    /// <summary>Đọc extended properties (class=1) cấp table + column cho user table.</summary>
    private static void ReadExtendedProperties(SqlConnection connection, DatabaseSchema schema)
    {
        const string sql = @"
SELECT
    s.name      AS SchemaName,
    t.name      AS TableName,
    c.name      AS ColumnName,        -- NULL nếu property cấp table
    ep.name     AS PropName,
    ep.value    AS PropValue
FROM sys.extended_properties ep
INNER JOIN sys.objects t  ON t.object_id = ep.major_id
INNER JOIN sys.schemas s  ON s.schema_id = t.schema_id
LEFT  JOIN sys.columns c  ON c.object_id = ep.major_id AND c.column_id = ep.minor_id
WHERE ep.class = 1                    -- class 1 = object hoặc column
  AND t.type = 'U'                    -- chỉ user table
  AND t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.name, ep.name;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var schemaName = reader.GetString(reader.GetOrdinal("SchemaName"));
            var tableName = reader.GetString(reader.GetOrdinal("TableName"));
            var tableKey = $"{schemaName}.{tableName}";

            if (!schema.Tables.TryGetValue(tableKey, out var table))
                continue;

            var columnName = GetNullableString(reader, "ColumnName");
            // ep.value là sql_variant → đọc dạng object rồi ToString
            var valueObj = GetNullableObject(reader, "PropValue");

            table.ExtendedProperties.Add(new ExtendedPropertyDef
            {
                Level = string.IsNullOrEmpty(columnName) ? "TABLE" : "COLUMN",
                TableSchema = schemaName,
                TableName = tableName,
                ColumnName = string.IsNullOrEmpty(columnName) ? null : columnName,
                Name = GetNullableString(reader, "PropName") ?? "",
                Value = valueObj?.ToString() ?? "",
            });
        }
    }

    /// <summary>Đọc sequences từ sys.sequences join sys.types. Các value là sql_variant → đọc dạng object.</summary>
    private static void ReadSequences(SqlConnection connection, DatabaseSchema schema)
    {
        const string sql = @"
SELECT
    s.name          AS SchemaName,
    sq.name         AS SequenceName,
    typ.name        AS DataType,
    sq.start_value  AS StartValue,
    sq.increment    AS Increment,
    sq.minimum_value AS MinValue,
    sq.maximum_value AS MaxValue,
    sq.is_cycling   AS IsCycling
FROM sys.sequences sq
INNER JOIN sys.schemas s  ON s.schema_id = sq.schema_id
INNER JOIN sys.types  typ ON typ.user_type_id = sq.user_type_id
ORDER BY s.name, sq.name;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var schemaName = reader.GetString(reader.GetOrdinal("SchemaName"));
            var seqName = reader.GetString(reader.GetOrdinal("SequenceName"));

            var seq = new SequenceDef
            {
                Schema = schemaName,
                Name = seqName,
                DataType = GetNullableString(reader, "DataType") ?? "",
                // start_value/increment/min/max là sql_variant → đọc object rồi ToString invariant
                StartValue = VariantToString(GetNullableObject(reader, "StartValue")) ?? "",
                Increment = VariantToString(GetNullableObject(reader, "Increment")) ?? "",
                MinValue = VariantToString(GetNullableObject(reader, "MinValue")),
                MaxValue = VariantToString(GetNullableObject(reader, "MaxValue")),
                IsCycling = GetBool(reader, "IsCycling"),
            };

            schema.Sequences[seq.Key] = seq;
        }
    }

    /// <summary>Đọc View/SP/Function/Trigger từ sys.sql_modules. Map đúng DbObjectType.</summary>
    private static void ReadProgrammables(SqlConnection connection, DatabaseSchema schema)
    {
        const string sql = @"
SELECT
    s.name      AS SchemaName,
    o.name      AS ObjectName,
    o.type      AS ObjectType,      -- V, P, FN, IF, TF, TR
    m.definition AS Definition
FROM sys.sql_modules m
INNER JOIN sys.objects o ON o.object_id = m.object_id
INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE o.is_ms_shipped = 0
  AND o.type IN ('V','P','FN','IF','TF','TR')
ORDER BY s.name, o.name;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var schemaName = reader.GetString(reader.GetOrdinal("SchemaName"));
            var objectName = reader.GetString(reader.GetOrdinal("ObjectName"));
            var rawType = (GetNullableString(reader, "ObjectType") ?? "").Trim();
            var definition = GetNullableString(reader, "Definition") ?? "";

            var obj = new ProgrammableObject
            {
                Schema = schemaName,
                Name = objectName,
                ObjectType = MapObjectType(rawType),
                Definition = definition,
            };

            schema.Programmables[obj.Key] = obj;
        }
    }

    /// <summary>Đọc synonyms từ sys.synonyms (base_object_name).</summary>
    private static void ReadSynonyms(SqlConnection connection, DatabaseSchema schema)
    {
        const string sql = @"
SELECT
    s.name      AS SchemaName,
    syn.name    AS SynonymName,
    syn.base_object_name AS BaseObject
FROM sys.synonyms syn
INNER JOIN sys.schemas s ON s.schema_id = syn.schema_id
WHERE syn.is_ms_shipped = 0
ORDER BY s.name, syn.name;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var schemaName = reader.GetString(reader.GetOrdinal("SchemaName"));
            var synName = reader.GetString(reader.GetOrdinal("SynonymName"));

            var syn = new SynonymDef
            {
                Schema = schemaName,
                Name = synName,
                BaseObject = GetNullableString(reader, "BaseObject") ?? "",
            };

            schema.Synonyms[syn.Key] = syn;
        }
    }

    /// <summary>
    /// Đọc user schemas từ sys.schemas. Lọc schema_id &lt; 16384 (user) và bỏ các schema hệ thống
    /// (dbo/sys/INFORMATION_SCHEMA/guest + các tên hệ thống khác).
    /// </summary>
    private static void ReadSchemas(SqlConnection connection, DatabaseSchema schema)
    {
        // schema_id < 16384: schema do user tạo (>= 16384 là role-owned schema hệ thống của SQL Server)
        const string sql = @"
SELECT s.name AS SchemaName
FROM sys.schemas s
WHERE s.schema_id < 16384
ORDER BY s.name;";

        // Tên schema hệ thống cần bỏ qua (không sinh CREATE/DROP SCHEMA)
        var systemSchemas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "dbo", "sys", "INFORMATION_SCHEMA", "guest",
        };

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var schemaName = reader.GetString(reader.GetOrdinal("SchemaName"));
            if (systemSchemas.Contains(schemaName)) continue;

            schema.Schemas[schemaName] = new SchemaDef { Name = schemaName };
        }
    }

    // ====================== SECURITY ======================

    /// <summary>
    /// Đọc database users + roles từ sys.database_principals.
    /// type='R' (is_fixed_role=0) → Role; type IN ('S','U','G','E','X') → User.
    /// Bỏ principal hệ thống (dbo/guest/public/sys/INFORMATION_SCHEMA + principal_id &lt;= 4).
    /// </summary>
    private static void ReadUsersAndRoles(SqlConnection connection, DatabaseSchema schema)
    {
        const string sql = @"
SELECT
    dp.name                     AS PrincipalName,
    dp.type                     AS PrincipalType,
    dp.type_desc                AS TypeDesc,
    dp.default_schema_name      AS DefaultSchema,
    dp.authentication_type_desc AS AuthDesc
FROM sys.database_principals dp
WHERE dp.is_fixed_role = 0
  AND dp.principal_id > 4
  AND dp.name NOT IN ('dbo','guest','public','sys','INFORMATION_SCHEMA')
  AND dp.type IN ('S','U','G','E','X','R')
ORDER BY dp.name;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var name = reader.GetString(reader.GetOrdinal("PrincipalName"));
            var type = (GetNullableString(reader, "PrincipalType") ?? "").Trim().ToUpperInvariant();

            if (type == "R")
            {
                // Role
                schema.Roles[name] = new DbRoleDef { Name = name };
            }
            else
            {
                // User (S/U/G/E/X)
                schema.Users[name] = new DbUserDef
                {
                    Name = name,
                    TypeDesc = GetNullableString(reader, "TypeDesc") ?? "",
                    DefaultSchema = GetNullableString(reader, "DefaultSchema"),
                    AuthenticationDesc = GetNullableString(reader, "AuthDesc") ?? "",
                };
            }
        }
    }

    /// <summary>
    /// Đọc role memberships từ sys.database_role_members → gán role vào DbUserDef.Roles
    /// (member là user đã đọc ở trên; role là role đã đọc ở trên).
    /// </summary>
    private static void ReadRoleMemberships(SqlConnection connection, DatabaseSchema schema)
    {
        const string sql = @"
SELECT
    r.name      AS RoleName,
    m.name      AS MemberName
FROM sys.database_role_members rm
INNER JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
INNER JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
ORDER BY m.name, r.name;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var roleName = GetNullableString(reader, "RoleName") ?? "";
            var memberName = GetNullableString(reader, "MemberName") ?? "";
            if (string.IsNullOrEmpty(memberName) || string.IsNullOrEmpty(roleName)) continue;

            // Chỉ gán nếu member là user đã được đọc (bỏ membership của system principals)
            if (schema.Users.TryGetValue(memberName, out var user) && !user.Roles.Contains(roleName))
                user.Roles.Add(roleName);
        }
    }

    /// <summary>
    /// Đọc permissions từ sys.database_permissions join sys.database_principals (grantee).
    /// TargetName theo class: DATABASE(0)→""; SCHEMA(3)→"[schema]"; OBJECT_OR_COLUMN(1)→"[schema].[obj]".
    /// Chỉ user principals (bỏ system); bỏ grantee 'public'.
    /// </summary>
    private static void ReadPermissions(SqlConnection connection, DatabaseSchema schema)
    {
        const string sql = @"
SELECT
    dp.state_desc       AS StateDesc,
    dp.permission_name  AS PermissionName,
    dp.class_desc       AS ClassDesc,
    dp.class            AS ClassId,
    dp.major_id         AS MajorId,
    pr.name             AS Grantee,
    CASE dp.class
        WHEN 3 THEN SCHEMA_NAME(dp.major_id)
        ELSE NULL
    END                 AS SchemaTarget,
    CASE dp.class
        WHEN 1 THEN OBJECT_SCHEMA_NAME(dp.major_id)
        ELSE NULL
    END                 AS ObjectSchema,
    CASE dp.class
        WHEN 1 THEN OBJECT_NAME(dp.major_id)
        ELSE NULL
    END                 AS ObjectName
FROM sys.database_permissions dp
INNER JOIN sys.database_principals pr ON pr.principal_id = dp.grantee_principal_id
WHERE pr.is_fixed_role = 0
  AND pr.principal_id > 4
  AND pr.name NOT IN ('dbo','guest','public','sys','INFORMATION_SCHEMA')
  AND pr.type IN ('S','U','G','E','X','R')
ORDER BY pr.name, dp.permission_name;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var grantee = GetNullableString(reader, "Grantee") ?? "";
            if (string.IsNullOrEmpty(grantee) ||
                string.Equals(grantee, "public", StringComparison.OrdinalIgnoreCase))
                continue;

            // class: 0=DATABASE, 1=OBJECT_OR_COLUMN, 3=SCHEMA → dựng TargetName tương ứng
            var classId = GetByte(reader, "ClassId");
            string target;
            switch (classId)
            {
                case 3: // SCHEMA
                    var schemaTarget = GetNullableString(reader, "SchemaTarget");
                    target = string.IsNullOrEmpty(schemaTarget) ? "" : $"[{schemaTarget}]";
                    break;
                case 1: // OBJECT_OR_COLUMN
                    var objSchema = GetNullableString(reader, "ObjectSchema");
                    var objName = GetNullableString(reader, "ObjectName");
                    target = string.IsNullOrEmpty(objName) ? "" : $"[{objSchema}].[{objName}]";
                    break;
                default: // DATABASE (0) hoặc class khác → không có target
                    target = "";
                    break;
            }

            schema.Permissions.Add(new DbPermissionDef
            {
                StateDesc = GetNullableString(reader, "StateDesc") ?? "",
                PermissionName = GetNullableString(reader, "PermissionName") ?? "",
                ClassDesc = GetNullableString(reader, "ClassDesc") ?? "",
                TargetName = target,
                Grantee = grantee,
            });
        }
    }

    // ====================== PARTITION ======================

    /// <summary>
    /// Đọc partition functions: sys.partition_functions join parameter type, kèm boundary values
    /// từ sys.partition_range_values (ORDER BY boundary_id). value là sql_variant → ToString invariant.
    /// </summary>
    private static void ReadPartitionFunctions(SqlConnection connection, DatabaseSchema schema)
    {
        // Tham số: type name + length nếu là kiểu chuỗi/binary có độ dài
        const string sql = @"
SELECT
    pf.name                     AS FunctionName,
    pf.boundary_value_on_right  AS RangeRight,
    t.name                      AS ParamTypeName,
    pp.max_length               AS ParamMaxLength,
    rv.value                    AS BoundaryValue,
    rv.boundary_id              AS BoundaryId
FROM sys.partition_functions pf
INNER JOIN sys.partition_parameters pp ON pp.function_id = pf.function_id
INNER JOIN sys.types t                 ON t.user_type_id = pp.user_type_id
LEFT  JOIN sys.partition_range_values rv ON rv.function_id = pf.function_id
ORDER BY pf.name, rv.boundary_id;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var name = reader.GetString(reader.GetOrdinal("FunctionName"));

            if (!schema.PartitionFunctions.TryGetValue(name, out var pf))
            {
                var typeName = (GetNullableString(reader, "ParamTypeName") ?? "").ToLowerInvariant();
                var maxLen = GetNullableInt16AsInt(reader, "ParamMaxLength");
                pf = new PartitionFunctionDef
                {
                    Name = name,
                    ParameterType = BuildParamTypeText(typeName, maxLen),
                    RangeRight = GetBool(reader, "RangeRight"),
                };
                schema.PartitionFunctions[name] = pf;
            }

            // BoundaryValue có thể NULL (function không có boundary) → bỏ qua dòng đó
            var boundary = VariantToString(GetNullableObject(reader, "BoundaryValue"));
            if (boundary != null)
                pf.BoundaryValues.Add(boundary);
        }
    }

    /// <summary>
    /// Đọc partition schemes: sys.partition_schemes join function name +
    /// destination_data_spaces → data_spaces (filegroup name, ORDER BY destination_id).
    /// </summary>
    private static void ReadPartitionSchemes(SqlConnection connection, DatabaseSchema schema)
    {
        const string sql = @"
SELECT
    ps.name     AS SchemeName,
    pf.name     AS FunctionName,
    ds.name     AS FilegroupName,
    dds.destination_id AS DestinationId
FROM sys.partition_schemes ps
INNER JOIN sys.partition_functions pf       ON pf.function_id = ps.function_id
INNER JOIN sys.destination_data_spaces dds  ON dds.partition_scheme_id = ps.data_space_id
INNER JOIN sys.data_spaces ds               ON ds.data_space_id = dds.data_space_id
ORDER BY ps.name, dds.destination_id;";

        using var cmd = new SqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var name = reader.GetString(reader.GetOrdinal("SchemeName"));

            if (!schema.PartitionSchemes.TryGetValue(name, out var ps))
            {
                ps = new PartitionSchemeDef
                {
                    Name = name,
                    FunctionName = GetNullableString(reader, "FunctionName") ?? "",
                };
                schema.PartitionSchemes[name] = ps;
            }

            var fg = GetNullableString(reader, "FilegroupName");
            if (!string.IsNullOrEmpty(fg))
                ps.Filegroups.Add(fg);
        }
    }

    /// <summary>Dựng text kiểu tham số partition function: thêm (len) cho kiểu chuỗi/binary có độ dài.</summary>
    private static string BuildParamTypeText(string typeName, int? maxLength)
    {
        switch (typeName)
        {
            case "varchar":
            case "char":
            case "varbinary":
            case "binary":
                if (maxLength.HasValue && maxLength.Value > 0)
                    return $"{typeName}({maxLength.Value})";
                return maxLength == -1 ? $"{typeName}(max)" : typeName;
            case "nvarchar":
            case "nchar":
                // nvarchar/nchar lưu max_length theo byte → chia 2 sang ký tự
                if (maxLength.HasValue && maxLength.Value > 0)
                    return $"{typeName}({maxLength.Value / 2})";
                return maxLength == -1 ? $"{typeName}(max)" : typeName;
            default:
                return typeName;
        }
    }

    /// <summary>Map mã type của sys.objects → DbObjectType.</summary>
    private static DbObjectType MapObjectType(string type) => type.ToUpperInvariant() switch
    {
        "V" => DbObjectType.View,
        "P" => DbObjectType.StoredProcedure,
        "FN" or "IF" or "TF" => DbObjectType.Function,
        "TR" => DbObjectType.Trigger,
        _ => DbObjectType.View,
    };

    // ---- Helpers đọc reader an toàn null ----

    private static bool GetBool(SqlDataReader reader, string col)
    {
        var i = reader.GetOrdinal(col);
        return !reader.IsDBNull(i) && reader.GetBoolean(i);
    }

    private static byte GetByte(SqlDataReader reader, string col)
    {
        var i = reader.GetOrdinal(col);
        return reader.IsDBNull(i) ? (byte)0 : reader.GetByte(i);
    }

    private static int? GetNullableInt16AsInt(SqlDataReader reader, string col)
    {
        var i = reader.GetOrdinal(col);
        return reader.IsDBNull(i) ? null : reader.GetInt16(i);
    }

    private static int? GetNullableByteAsInt(SqlDataReader reader, string col)
    {
        var i = reader.GetOrdinal(col);
        return reader.IsDBNull(i) ? null : reader.GetByte(i);
    }

    private static string? GetNullableString(SqlDataReader reader, string col)
    {
        var i = reader.GetOrdinal(col);
        return reader.IsDBNull(i) ? null : reader.GetString(i);
    }

    /// <summary>Đọc 1 cột dạng object (vd sql_variant), null-safe.</summary>
    private static object? GetNullableObject(SqlDataReader reader, string col)
    {
        var i = reader.GetOrdinal(col);
        return reader.IsDBNull(i) ? null : reader.GetValue(i);
    }

    /// <summary>Đổi value sql_variant về string theo invariant culture (number không bị format theo locale).</summary>
    private static string? VariantToString(object? value)
    {
        if (value is null) return null;
        return value is IFormattable f
            ? f.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
            : value.ToString();
    }
}
