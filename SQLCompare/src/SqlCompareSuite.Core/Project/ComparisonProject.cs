using System.Text.Json;
using System.Text.Json.Serialization;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.Core.Project;

/// <summary>
/// Cấu hình 1 lần so sánh (schema hoặc data) — có thể lưu/đọc lại từ file.
/// Tương tự .scp/.sdc của Redgate. Đuôi file gợi ý: .sqlcmp (schema) / .sqldcmp (data).
/// </summary>
public sealed class ComparisonProject
{
    /// <summary>Loại so sánh: "Schema" | "Data".</summary>
    public string Kind { get; set; } = "Schema";

    /// <summary>Database nguồn.</summary>
    public DbConnectionInfo Source { get; set; } = new();

    /// <summary>Database đích.</summary>
    public DbConnectionInfo Target { get; set; } = new();

    /// <summary>Bảng được chọn — dùng cho Data Compare.</summary>
    public string? SelectedTable { get; set; }

    /// <summary>Điều kiện lọc data (optional, WHERE clause).</summary>
    public string? WhereClause { get; set; }

    /// <summary>Phiên bản định dạng file, phục vụ migrate về sau.</summary>
    public string FileFormatVersion { get; set; } = "1.0";
}

/// <summary>
/// Lưu/đọc <see cref="ComparisonProject"/> ra/từ file JSON (UTF-8, indented).
/// Mật khẩu được mã hóa bằng Windows DPAPI (CurrentUser) — xem <see cref="SecretProtector"/>.
/// </summary>
public static class ComparisonProjectStore
{
    // Cấu hình JSON dùng chung: indented, bỏ qua property null, không phân biệt hoa thường khi đọc.
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Lưu project ra file JSON. Tạo thư mục cha nếu chưa có.</summary>
    public static void Save(string path, ComparisonProject project)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Đường dẫn file không được rỗng.", nameof(path));
        ArgumentNullException.ThrowIfNull(project);

        // Đảm bảo thư mục cha tồn tại trước khi ghi.
        var dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            System.IO.Directory.CreateDirectory(dir);

        // Mã hóa password (DPAPI) trên BẢN SAO trước khi ghi — không đụng object gốc.
        var toWrite = new ComparisonProject
        {
            Kind = project.Kind,
            Source = SecretProtector.CloneWithPassword(project.Source, SecretProtector.Protect(project.Source?.Password)),
            Target = SecretProtector.CloneWithPassword(project.Target, SecretProtector.Protect(project.Target?.Password)),
            SelectedTable = project.SelectedTable,
            WhereClause = project.WhereClause,
            FileFormatVersion = project.FileFormatVersion,
        };

        var json = JsonSerializer.Serialize(toWrite, Options);
        System.IO.File.WriteAllText(path, json, System.Text.Encoding.UTF8);
    }

    /// <summary>Đọc project từ file JSON. Lỗi đọc/parse/null → InvalidOperationException.</summary>
    public static ComparisonProject Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Đường dẫn file không được rỗng.", nameof(path));

        if (!System.IO.File.Exists(path))
            throw new InvalidOperationException($"Không tìm thấy file project: '{path}'.");

        string json;
        try
        {
            json = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Không đọc được file project: '{path}'.", ex);
        }

        ComparisonProject? project;
        try
        {
            project = JsonSerializer.Deserialize<ComparisonProject>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"File project sai định dạng JSON: '{path}'.", ex);
        }

        if (project is null)
            throw new InvalidOperationException($"File project rỗng hoặc không hợp lệ: '{path}'.");

        // Null-safe + giải mã password (DPAPI).
        project.Source ??= new DbConnectionInfo();
        project.Target ??= new DbConnectionInfo();
        project.Source.Password = SecretProtector.Unprotect(project.Source.Password);
        project.Target.Password = SecretProtector.Unprotect(project.Target.Password);

        return project;
    }
}
