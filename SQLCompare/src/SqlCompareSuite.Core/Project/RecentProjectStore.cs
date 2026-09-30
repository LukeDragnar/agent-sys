using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlCompareSuite.Core.Project;

/// <summary>
/// 1 project so sánh đã mở/lưu gần đây — chỉ giữ đường dẫn file + loại + tên hiển thị.
/// Connection/bảng được đọc lại từ file khi mở (qua <see cref="ComparisonProjectStore"/>).
/// </summary>
public sealed class RecentProject
{
    public string Path = "";
    public string Kind = "Schema";
    public string Name = "";
    /// <summary>Tên group chứa project ("" = chưa nhóm).</summary>
    public string Group = "";
}

/// <summary>
/// Lưu/đọc danh sách project gần đây ra file JSON trong
/// %APPDATA%\SqlCompareSuite\recent_projects.json.
/// Mọi lỗi (file hỏng, không đọc/ghi được...) đều nuốt → trả list rỗng, KHÔNG ném ra UI.
/// </summary>
public static class RecentProjectStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true, // RecentProject dùng public field, không phải property.
    };

    private const int MaxItems = 50;

    /// <summary>Raise sau mỗi Add/Remove để UI tự refresh danh sách.</summary>
    public static event System.Action? Changed;

    /// <summary>Đường dẫn file: %APPDATA%\SqlCompareSuite\recent_projects.json.</summary>
    private static string FilePath()
    {
        string dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SqlCompareSuite");
        return System.IO.Path.Combine(dir, "recent_projects.json");
    }

    /// <summary>Đọc danh sách. Lỗi/không có file → list rỗng.</summary>
    public static List<RecentProject> Load()
    {
        try
        {
            string path = FilePath();
            if (!System.IO.File.Exists(path))
                return new List<RecentProject>();

            string json = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
            var items = JsonSerializer.Deserialize<List<RecentProject>>(json, Options);
            if (items == null) return new List<RecentProject>();

            foreach (var it in items)
            {
                it.Path ??= "";
                it.Kind = string.IsNullOrWhiteSpace(it.Kind) ? "Schema" : it.Kind;
                it.Name ??= "";
                it.Group ??= "";
            }
            return items;
        }
        catch
        {
            return new List<RecentProject>();
        }
    }

    /// <summary>Ghi cả danh sách ra file. Tạo thư mục nếu chưa có. Lỗi → nuốt im lặng.</summary>
    private static void Save(List<RecentProject> items)
    {
        try
        {
            items ??= new List<RecentProject>();
            string path = FilePath();
            string? dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                System.IO.Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(items, Options);
            System.IO.File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        }
        catch
        {
            // Không ném ra UI.
        }
    }

    /// <summary>
    /// Thêm/đưa 1 project lên đầu danh sách. Dedup theo Path (OrdinalIgnoreCase),
    /// mới nhất lên đầu, cap <see cref="MaxItems"/>. Name = tên file không đuôi.
    /// Raise <see cref="Changed"/> sau khi xong.
    /// </summary>
    public static void Add(string path, string kind)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        var items = Load();
        // Bỏ entry cũ cùng path (dedup).
        items.RemoveAll(x =>
            string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));

        string name;
        try { name = System.IO.Path.GetFileNameWithoutExtension(path); }
        catch { name = path; }

        // Mới nhất lên đầu.
        items.Insert(0, new RecentProject
        {
            Path = path,
            Kind = string.IsNullOrWhiteSpace(kind) ? "Schema" : kind,
            Name = name,
        });

        // Cap số lượng.
        if (items.Count > MaxItems)
            items = items.GetRange(0, MaxItems);

        Save(items);
        Changed?.Invoke();
    }

    /// <summary>Xóa theo Path (OrdinalIgnoreCase) rồi raise <see cref="Changed"/>.</summary>
    public static void Remove(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        var items = Load();
        int removed = items.RemoveAll(x =>
            string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));
        Save(items);
        if (removed > 0) Changed?.Invoke();
    }

    // ====================== Groups (phẳng 1 cấp) ======================

    private static string GroupsFilePath()
    {
        string dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SqlCompareSuite");
        return System.IO.Path.Combine(dir, "project_groups.json");
    }

    /// <summary>Danh sách tên group đã tạo (kể cả group rỗng chưa có project).</summary>
    public static List<string> LoadGroups()
    {
        try
        {
            string path = GroupsFilePath();
            if (!System.IO.File.Exists(path)) return new List<string>();
            var json = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
            return JsonSerializer.Deserialize<List<string>>(json, Options) ?? new List<string>();
        }
        catch { return new List<string>(); }
    }

    private static void SaveGroups(List<string> groups)
    {
        try
        {
            groups ??= new List<string>();
            string path = GroupsFilePath();
            string? dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(path, JsonSerializer.Serialize(groups, Options), System.Text.Encoding.UTF8);
        }
        catch { }
    }

    /// <summary>Tạo group mới (nếu chưa có). Raise Changed.</summary>
    public static void AddGroup(string name)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0) return;
        var groups = LoadGroups();
        if (!groups.Any(g => string.Equals(g, name, StringComparison.OrdinalIgnoreCase)))
        {
            groups.Add(name);
            SaveGroups(groups);
            Changed?.Invoke();
        }
    }

    /// <summary>Xoá group: bỏ tên + gỡ các project khỏi group đó (về chưa nhóm). Raise Changed.</summary>
    public static void RemoveGroup(string name)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0) return;
        var groups = LoadGroups();
        groups.RemoveAll(g => string.Equals(g, name, StringComparison.OrdinalIgnoreCase));
        SaveGroups(groups);
        var items = Load();
        foreach (var it in items)
            if (string.Equals(it.Group, name, StringComparison.OrdinalIgnoreCase)) it.Group = "";
        Save(items);
        Changed?.Invoke();
    }

    /// <summary>Gán project (theo Path) vào group ("" = bỏ nhóm). Tự tạo group nếu chưa có. Raise Changed.</summary>
    public static void SetGroup(string path, string group)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        group = (group ?? "").Trim();
        var items = Load();
        var it = items.FirstOrDefault(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));
        if (it == null) return;
        it.Group = group;
        Save(items);
        if (group.Length > 0)
        {
            var groups = LoadGroups();
            if (!groups.Any(g => string.Equals(g, group, StringComparison.OrdinalIgnoreCase)))
            { groups.Add(group); SaveGroups(groups); }
        }
        Changed?.Invoke();
    }
}
