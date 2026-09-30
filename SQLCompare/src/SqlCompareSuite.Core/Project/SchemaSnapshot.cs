using System.Text.Json;
using System.Text.Json.Serialization;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Project;

/// <summary>
/// Lưu / nạp snapshot toàn bộ DatabaseSchema ra/từ file JSON.
/// Dùng để chụp schema 1 thời điểm rồi compare với database thật sau này.
/// </summary>
public static class SchemaSnapshotStore
{
    // Options dùng chung: indent đẹp, bỏ qua null, đọc không phân biệt hoa thường tên property.
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Serialize DatabaseSchema ra file JSON (indent).</summary>
    public static void Save(string path, DatabaseSchema schema)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Đường dẫn snapshot rỗng.", nameof(path));
        if (schema is null)
            throw new ArgumentNullException(nameof(schema));

        // Đảm bảo thư mục đích tồn tại.
        var dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(schema, Options);
        System.IO.File.WriteAllText(path, json);
    }

    /// <summary>Đọc file JSON và dựng lại DatabaseSchema. Lỗi/null → InvalidOperationException.</summary>
    public static DatabaseSchema Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Đường dẫn snapshot rỗng.");
        if (!System.IO.File.Exists(path))
            throw new InvalidOperationException($"Không tìm thấy file snapshot: {path}");

        string json;
        try
        {
            json = System.IO.File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Không đọc được file snapshot '{path}': {ex.Message}", ex);
        }

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException($"File snapshot rỗng: {path}");

        DatabaseSchema? schema;
        try
        {
            schema = JsonSerializer.Deserialize<DatabaseSchema>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"File snapshot không hợp lệ (JSON lỗi) '{path}': {ex.Message}", ex);
        }

        if (schema is null)
            throw new InvalidOperationException($"File snapshot deserialize ra null: {path}");

        // System.Text.Json populate dictionary qua Add nhưng KHÔNG giữ comparer OrdinalIgnoreCase.
        // Dựng lại schema với comparer đúng để lookup không phân biệt hoa thường (như khi đọc từ DB).
        return Normalize(schema);
    }

    /// <summary>Tạo DatabaseSchema mới với các dictionary dùng đúng comparer OrdinalIgnoreCase.</summary>
    private static DatabaseSchema Normalize(DatabaseSchema raw)
    {
        var result = new DatabaseSchema();

        // BUG-FIX: trước đây chỉ copy Tables/Programmables/Sequences → các collection mới
        // bị bỏ sót khi load snapshot, khiến compare báo OnlyInTarget SAI. Copy ĐẦY ĐỦ mọi
        // collection hiện có của DatabaseSchema (đồng bộ với SchemaModels.cs).
        CopyInto(result.Tables, raw.Tables);
        CopyInto(result.Programmables, raw.Programmables);
        CopyInto(result.Sequences, raw.Sequences);
        CopyInto(result.Synonyms, raw.Synonyms);
        CopyInto(result.Schemas, raw.Schemas);

        // SECURITY
        CopyInto(result.Users, raw.Users);
        CopyInto(result.Roles, raw.Roles);
        CopyList(result.Permissions, raw.Permissions);

        // PARTITION
        CopyInto(result.PartitionFunctions, raw.PartitionFunctions);
        CopyInto(result.PartitionSchemes, raw.PartitionSchemes);

        return result;
    }

    // Copy phần tử từ dictionary nguồn vào dictionary đích (đã có comparer mong muốn).
    private static void CopyInto<T>(Dictionary<string, T> dest, Dictionary<string, T>? source)
    {
        if (source is null) return;
        foreach (var kv in source)
            dest[kv.Key] = kv.Value;
    }

    // Copy phần tử từ list nguồn vào list đích (cho collection không phải dictionary, vd Permissions).
    private static void CopyList<T>(List<T> dest, List<T>? source)
    {
        if (source is null) return;
        dest.AddRange(source);
    }
}
