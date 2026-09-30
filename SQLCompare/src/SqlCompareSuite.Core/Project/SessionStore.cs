using System.Text.Json;
using System.Text.Json.Serialization;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Project;

/// <summary>1 tab đang mở (để khôi phục phiên làm việc lần sau).</summary>
public sealed class SessionTab
{
    public string Kind { get; set; } = "Schema";       // "Schema" | "Data"
    public DbConnectionInfo Source { get; set; } = new();
    public DbConnectionInfo Target { get; set; } = new();
    public string? SelectedTable { get; set; }          // cho Data Compare
    public string Title { get; set; } = "";
    public bool Pinned { get; set; }                     // tab đã ghim (giữ qua phiên)
}

/// <summary>
/// Lưu/đọc danh sách tab đang mở → khôi phục khi mở app lại.
/// File: %APPDATA%\SqlCompareSuite\session.json. Password mã hóa DPAPI (<see cref="SecretProtector"/>).
/// </summary>
public static class SessionStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    private static string FilePath()
    {
        string dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SqlCompareSuite");
        return System.IO.Path.Combine(dir, "session.json");
    }

    public static List<SessionTab> Load()
    {
        try
        {
            string path = FilePath();
            if (!System.IO.File.Exists(path)) return new List<SessionTab>();
            string json = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
            var tabs = JsonSerializer.Deserialize<List<SessionTab>>(json, Options) ?? new List<SessionTab>();
            foreach (var t in tabs)
            {
                t.Source ??= new DbConnectionInfo();
                t.Target ??= new DbConnectionInfo();
                t.Source.Password = SecretProtector.Unprotect(t.Source.Password);
                t.Target.Password = SecretProtector.Unprotect(t.Target.Password);
            }
            return tabs;
        }
        catch
        {
            return new List<SessionTab>();
        }
    }

    public static void Save(List<SessionTab> tabs)
    {
        try
        {
            tabs ??= new List<SessionTab>();
            // Mã hóa password trên bản sao trước khi ghi.
            var toWrite = tabs.Select(t => new SessionTab
            {
                Kind = t.Kind,
                Source = SecretProtector.CloneWithPassword(t.Source, SecretProtector.Protect(t.Source?.Password)),
                Target = SecretProtector.CloneWithPassword(t.Target, SecretProtector.Protect(t.Target?.Password)),
                SelectedTable = t.SelectedTable,
                Title = t.Title,
                Pinned = t.Pinned,
            }).ToList();

            string path = FilePath();
            string? dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(path, JsonSerializer.Serialize(toWrite, Options), System.Text.Encoding.UTF8);
        }
        catch
        {
            // nuốt
        }
    }
}
