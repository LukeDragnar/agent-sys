using Microsoft.Extensions.Configuration;

namespace AgentCore.Services;

/// <summary>
/// Load và ghép system prompt từ agents/ + rules/ + projects/.
/// Dùng được cho cả AgentService (interactive) lẫn OrchestratorService (automated).
/// </summary>
public class AgentLoader : IAgentLoader
{
    private readonly string _baseDir;

    public AgentLoader(IConfiguration config)
    {
        _baseDir = config["AgentSystem:BaseDir"]
            ?? throw new InvalidOperationException("AgentSystem:BaseDir is not configured.");
    }

    public async Task<string> BuildSystemPrompt(string agentType, string? projectKey = null)
    {
        var parts = new List<string>();

        // 1. Load agent definition
        var agentContent = await LoadDefinition(agentType);
        parts.Add(agentContent);

        // 2. Load rule files từ ## Rules section
        var ruleFiles = ParseListSection(agentContent, "Rules");
        foreach (var relPath in ruleFiles)
        {
            var fullPath = Path.Combine(_baseDir, relPath);
            if (File.Exists(fullPath))
                parts.Add(await File.ReadAllTextAsync(fullPath));
        }

        // 3. Load project context files nếu có projectKey
        if (!string.IsNullOrWhiteSpace(projectKey))
        {
            var projectFiles = ResolveProjectFiles(agentContent, projectKey);
            foreach (var relPath in projectFiles)
            {
                var fullPath = Path.Combine(_baseDir, relPath);
                if (File.Exists(fullPath))
                    parts.Add(await File.ReadAllTextAsync(fullPath));
            }
        }

        return string.Join("\n\n---\n\n", parts);
    }

    public async Task<string> LoadDefinition(string agentType)
    {
        var path = Path.Combine(_baseDir, "agents", $"{agentType}.md");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Agent definition not found: {path}");
        return await File.ReadAllTextAsync(path);
    }

    // Parse bullet list dưới một heading ## {sectionName}
    // Dừng khi gặp heading ## tiếp theo
    private static List<string> ParseListSection(string markdown, string sectionName)
    {
        var results = new List<string>();
        var inSection = false;

        foreach (var line in markdown.Split('\n'))
        {
            if (line.StartsWith("## "))
            {
                inSection = line.TrimEnd().Equals($"## {sectionName}", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (inSection && line.TrimStart().StartsWith("- "))
                results.Add(line.TrimStart()[2..].Trim());
        }

        return results;
    }

    // Từ ## Project Context, lấy file path theo projectKey
    // Agent definition dùng format: - projects/{key}/file.md
    private static List<string> ResolveProjectFiles(string agentContent, string projectKey)
    {
        var allProjectLines = ParseListSection(agentContent, "Project Context");

        // Lọc chỉ các dòng thuộc projectKey hoặc dạng projects/*/file.md
        return allProjectLines
            .Where(l => l.StartsWith($"projects/{projectKey}/", StringComparison.OrdinalIgnoreCase)
                     || l.StartsWith("projects/*/"))
            .Select(l => l.Replace("projects/*/", $"projects/{projectKey}/"))
            .ToList();
    }
}
