using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AgentInfrastructure.Entities;
using AgentInfrastructure.Repositories;
using Microsoft.Extensions.Configuration;

namespace AgentCore.Services;

public class ProjectService : IProjectService
{
    private static readonly string SCAN_PROMPT = LoadPrompt("scan-project-agent.txt");
    private const string Model = "claude-haiku-4-5-20251001";
    private const string Endpoint = "https://api.anthropic.com/v1/messages";

    private readonly IProjectRepository _repo;
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public ProjectService(IProjectRepository repo, HttpClient http, IConfiguration config)
    {
        _repo = repo;
        _http = http;
        _apiKey = config["Claude:ApiKey"] ?? throw new InvalidOperationException("Claude:ApiKey missing.");
    }

    public Task<List<Project>> GetAll() => _repo.GetAll();
    public Task<Project?> GetById(Guid id) => _repo.GetById(id);
    public Task<Project> Create(string name, string description, Guid userId) => _repo.Create(name, description, userId);
    public Task<Project?> Update(Guid id, string name, string description) => _repo.Update(id, name, description);
    public Task Delete(Guid id) => _repo.Delete(id);
    public Task<UserProjectConfig?> GetUserConfig(Guid projectId, Guid userId) => _repo.GetUserConfig(projectId, userId);
    public Task<UserProjectConfig> UpsertUserConfig(Guid projectId, Guid userId, string bePath, string fePath, string outputDir, string? modulesJson = null)
        => _repo.UpsertUserConfig(projectId, userId, bePath, fePath, outputDir, modulesJson);

    public async Task<string> ScanAsync(Guid projectId, string bePath, string fePath, Guid userId)
    {
        var samples = new StringBuilder();
        samples.AppendLine("=== BACKEND SAMPLES ===");
        AppendSamples(samples, bePath, new[] { ".cs" }, new[] { "Controller", "Service", "Repository", "Entity", "DbContext" });
        samples.AppendLine("=== FRONTEND SAMPLES ===");
        AppendSamples(samples, fePath, new[] { ".ts", ".html" }, new[] { "component", "service", "module" });

        var profileJson = await CallClaude(samples.ToString());
        await _repo.UpdateProfile(projectId, profileJson, userId);
        return profileJson;
    }

    public string BuildProfilePrompt(string? profileJson)
    {
        if (string.IsNullOrWhiteSpace(profileJson)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(profileJson);
            var root = doc.RootElement;
            var sb = new StringBuilder();
            sb.AppendLine("## PROJECT CONVENTIONS");

            if (root.TryGetProperty("stack", out var stack))
                sb.AppendLine($"Stack: {stack}");
            if (root.TryGetProperty("backend", out var be))
                sb.AppendLine($"Backend: {be}");
            if (root.TryGetProperty("frontend", out var fe))
                sb.AppendLine($"Frontend: {fe}");
            if (root.TryGetProperty("conventions", out var conv))
                sb.AppendLine($"Conventions: {conv}");
            if (root.TryGetProperty("codeExamples", out var ex))
                sb.AppendLine($"Examples: {ex}");

            return sb.ToString();
        }
        catch { return profileJson; }
    }

    private static void AppendSamples(StringBuilder sb, string dir, string[] extensions, string[] keywords)
    {
        if (!Directory.Exists(dir)) return;
        var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Where(f => keywords.Any(k => Path.GetFileName(f).Contains(k, StringComparison.OrdinalIgnoreCase)))
            .Take(8);

        foreach (var file in files)
        {
            try
            {
                var lines = File.ReadAllLines(file).Take(80);
                sb.AppendLine($"--- {Path.GetFileName(file)} ---");
                sb.AppendLine(string.Join('\n', lines));
            }
            catch { }
        }
    }

    private async Task<string> CallClaude(string content)
    {
        var body = new
        {
            model = Model,
            max_tokens = 2000,
            system = SCAN_PROMPT,
            messages = new[] { new { role = "user", content } }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "{}";
    }

    private static string LoadPrompt(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Prompts", fileName);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
