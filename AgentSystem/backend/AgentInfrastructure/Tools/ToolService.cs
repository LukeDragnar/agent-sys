using System.Text;
using System.Text.Json;
using AgentInfrastructure.Clients;
using Microsoft.Extensions.Configuration;

namespace AgentInfrastructure.Tools;

public class ToolService : IToolService
{
    private readonly string _rootPath;
    private const int MaxFileSize = 50_000;   // chars
    private const int MaxSearchResults = 10;
    private const int MaxLinesContext = 5;

    public ToolService(IConfiguration config)
    {
        _rootPath = config["Tools:ScrmPath"] ?? @"D:\PROJECT\Git\scrm";
    }

    public List<ToolDefinition> GetDefinitions() => new()
    {
        new ToolDefinition
        {
            Name = "search_code",
            Description = "Tìm kiếm text/pattern trong codebase SCRM. Trả về các đoạn code liên quan kèm file path và số dòng.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string", description = "Từ khóa hoặc tên method/class/variable cần tìm" },
                    extension = new { type = "string", description = "Lọc theo đuôi file, ví dụ: .cs .ts .json (để trống = tất cả)" },
                    module = new { type = "string", description = "Tên module/folder con trong SCRM để thu hẹp phạm vi tìm kiếm" }
                },
                required = new[] { "query" }
            }
        },
        new ToolDefinition
        {
            Name = "read_file",
            Description = "Đọc nội dung một file trong codebase SCRM.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Đường dẫn file tương đối hoặc tuyệt đối trong SCRM" }
                },
                required = new[] { "path" }
            }
        },
        new ToolDefinition
        {
            Name = "list_directory",
            Description = "Liệt kê files và folders trong một thư mục của SCRM.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Đường dẫn folder (để trống = root SCRM)" },
                    pattern = new { type = "string", description = "Filter pattern, ví dụ: *.cs (để trống = tất cả)" }
                },
                required = Array.Empty<string>()
            }
        }
    };

    public async Task<string> Execute(string toolName, JsonElement input)
    {
        return toolName switch
        {
            "search_code" => await SearchCode(input),
            "read_file" => await ReadFile(input),
            "list_directory" => ListDirectory(input),
            _ => $"Unknown tool: {toolName}"
        };
    }

    private Task<string> SearchCode(JsonElement input)
    {
        var query = input.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
        var extension = input.TryGetProperty("extension", out var e) ? e.GetString() ?? "" : "";
        var module = input.TryGetProperty("module", out var m) ? m.GetString() ?? "" : "";

        if (string.IsNullOrWhiteSpace(query)) return Task.FromResult("query is required");

        var searchRoot = string.IsNullOrWhiteSpace(module)
            ? _rootPath
            : Path.Combine(_rootPath, module);

        if (!Directory.Exists(searchRoot))
            return Task.FromResult($"Directory not found: {searchRoot}");

        var pattern = string.IsNullOrWhiteSpace(extension) ? "*.*" : $"*{extension}";
        var files = Directory.EnumerateFiles(searchRoot, pattern, SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\.git\\"))
            .ToList();

        var results = new StringBuilder();
        int found = 0;

        foreach (var file in files)
        {
            if (found >= MaxSearchResults) break;
            try
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length && found < MaxSearchResults; i++)
                {
                    if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        var relPath = Path.GetRelativePath(_rootPath, file);
                        var start = Math.Max(0, i - MaxLinesContext);
                        var end = Math.Min(lines.Length - 1, i + MaxLinesContext);
                        results.AppendLine($"### {relPath}:{i + 1}");
                        for (int j = start; j <= end; j++)
                        {
                            var marker = j == i ? ">>>" : "   ";
                            results.AppendLine($"{marker} {j + 1}: {lines[j]}");
                        }
                        results.AppendLine();
                        found++;
                    }
                }
            }
            catch { /* skip unreadable files */ }
        }

        return Task.FromResult(found == 0
            ? $"Không tìm thấy '{query}' trong {searchRoot}"
            : $"Tìm thấy {found} kết quả cho '{query}':\n\n{results}");
    }

    private Task<string> ReadFile(JsonElement input)
    {
        var path = input.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(path)) return Task.FromResult("path is required");

        // Resolve path — allow relative to SCRM root or absolute within SCRM
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(_rootPath, path);
        fullPath = Path.GetFullPath(fullPath);

        // Security: only allow files within SCRM root
        if (!fullPath.StartsWith(Path.GetFullPath(_rootPath), StringComparison.OrdinalIgnoreCase))
            return Task.FromResult("Access denied: path is outside SCRM root");

        if (!File.Exists(fullPath))
            return Task.FromResult($"File not found: {fullPath}");

        var content = File.ReadAllText(fullPath);
        if (content.Length > MaxFileSize)
            content = content[..MaxFileSize] + $"\n\n... [truncated, total {content.Length} chars]";

        var relPath = Path.GetRelativePath(_rootPath, fullPath);
        return Task.FromResult($"### {relPath}\n\n```\n{content}\n```");
    }

    private string ListDirectory(JsonElement input)
    {
        var path = input.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
        var pattern = input.TryGetProperty("pattern", out var pt) ? pt.GetString() ?? "*" : "*";

        var fullPath = string.IsNullOrWhiteSpace(path)
            ? _rootPath
            : Path.IsPathRooted(path) ? path : Path.Combine(_rootPath, path);
        fullPath = Path.GetFullPath(fullPath);

        if (!fullPath.StartsWith(Path.GetFullPath(_rootPath), StringComparison.OrdinalIgnoreCase))
            return "Access denied: path is outside SCRM root";

        if (!Directory.Exists(fullPath))
            return $"Directory not found: {fullPath}";

        var sb = new StringBuilder();
        var relPath = Path.GetRelativePath(_rootPath, fullPath);
        sb.AppendLine($"### {(relPath == "." ? "SCRM root" : relPath)}");

        foreach (var dir in Directory.GetDirectories(fullPath).Take(50))
            sb.AppendLine($"📁 {Path.GetFileName(dir)}/");

        foreach (var file in Directory.GetFiles(fullPath, pattern).Take(100))
            sb.AppendLine($"📄 {Path.GetFileName(file)}");

        return sb.ToString();
    }
}
