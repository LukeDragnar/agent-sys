using System.Text.Json;

namespace SqlCompareSuite.Core.Project;

/// <summary>
/// Danh sách SERVER đã dùng gần đây (gợi ý "Recently used" cho ô Server).
/// Lưu JSON tại %APPDATA%\SqlCompareSuite\recent_servers.json. Mọi lỗi nuốt → list rỗng.
/// </summary>
public static class RecentServerStore
{
    private const int MaxItems = 20;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private static string FilePath()
    {
        string dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SqlCompareSuite");
        return System.IO.Path.Combine(dir, "recent_servers.json");
    }

    /// <summary>Đọc danh sách (mới nhất lên đầu). Lỗi → rỗng.</summary>
    public static List<string> Load()
    {
        try
        {
            string path = FilePath();
            if (!System.IO.File.Exists(path)) return new List<string>();
            string json = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>Thêm server lên đầu (dedup không phân biệt hoa thường), cap 20, rồi lưu.</summary>
    public static void Add(string? server)
    {
        if (string.IsNullOrWhiteSpace(server)) return;
        server = server.Trim();
        try
        {
            var list = Load();
            list.RemoveAll(x => string.Equals(x, server, StringComparison.OrdinalIgnoreCase));
            list.Insert(0, server);
            if (list.Count > MaxItems) list = list.GetRange(0, MaxItems);

            string path = FilePath();
            string? dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(path, JsonSerializer.Serialize(list, Options), System.Text.Encoding.UTF8);
        }
        catch
        {
            // nuốt — không ảnh hưởng UI
        }
    }
}
