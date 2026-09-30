using System.Text.Json;
using System.Text.Json.Serialization;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Project;

/// <summary>1 connection đã đặt tên, lưu lại để tái dùng nhanh giữa các phiên.</summary>
public sealed class SavedConnection
{
    public string Name = "";
    public DbConnectionInfo Connection = new();
}

/// <summary>
/// Lưu/đọc danh sách connection đã đặt tên ra file JSON trong
/// %APPDATA%\SqlCompareSuite\connections.json.
/// Mọi lỗi (file hỏng, không đọc được...) đều nuốt và trả list rỗng — KHÔNG ném ra UI.
/// </summary>
public static class SavedConnectionStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true, // SavedConnection dùng public field, không phải property.
    };

    /// <summary>Đường dẫn file: %APPDATA%\SqlCompareSuite\connections.json.</summary>
    private static string FilePath()
    {
        string dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SqlCompareSuite");
        return System.IO.Path.Combine(dir, "connections.json");
    }

    /// <summary>Đọc danh sách. Lỗi/không có file → list rỗng.</summary>
    public static List<SavedConnection> Load()
    {
        try
        {
            string path = FilePath();
            if (!System.IO.File.Exists(path))
                return new List<SavedConnection>();

            string json = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
            var items = JsonSerializer.Deserialize<List<SavedConnection>>(json, Options);
            if (items == null) return new List<SavedConnection>();

            // Null-safe sau deserialize + giải mã password (DPAPI).
            foreach (var it in items)
            {
                it.Name ??= "";
                it.Connection ??= new DbConnectionInfo();
                it.Connection.Password = SecretProtector.Unprotect(it.Connection.Password);
            }
            return items;
        }
        catch
        {
            return new List<SavedConnection>();
        }
    }

    /// <summary>Ghi cả danh sách ra file. Tạo thư mục nếu chưa có. Lỗi → nuốt im lặng.</summary>
    public static void Save(List<SavedConnection> items)
    {
        try
        {
            items ??= new List<SavedConnection>();
            string path = FilePath();
            string? dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                System.IO.Directory.CreateDirectory(dir);

            // Mã hóa password (DPAPI) trên BẢN SAO trước khi ghi — không đụng object gốc của caller.
            var toWrite = items.Select(it => new SavedConnection
            {
                Name = it.Name,
                Connection = SecretProtector.CloneWithPassword(
                    it.Connection, SecretProtector.Protect(it.Connection?.Password)),
            }).ToList();

            string json = JsonSerializer.Serialize(toWrite, Options);
            System.IO.File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        }
        catch
        {
            // Không ném ra UI.
        }
    }

    /// <summary>Thêm/cập nhật theo Name (case-insensitive) rồi Save.</summary>
    public static void Add(SavedConnection item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Name)) return;

        var items = Load();
        int idx = items.FindIndex(x =>
            string.Equals(x.Name, item.Name, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
            items[idx] = item;
        else
            items.Add(item);

        Save(items);
    }

    /// <summary>Xóa theo Name (case-insensitive) rồi Save.</summary>
    public static void Remove(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var items = Load();
        items.RemoveAll(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        Save(items);
    }
}
