using System.Text.Json;
using System.Text.RegularExpressions;
using AgentInfrastructure.Clients;
using AgentInfrastructure.Entities;
using AgentInfrastructure.Repositories;

namespace AgentCore.Services;

public class SpecService : ISpecService
{
    private readonly ISpecRepository _repo;
    private static readonly string SplitterPrompt = LoadPrompt("splitter-agent.txt");

    public SpecService(ISpecRepository repo) { _repo = repo; }

    public async Task<SpecTaskVersion?> TryExtractAndSave(Guid sessionId, string response, string moduleName = "General")
    {
        var raw = ExtractJsonRaw(response);
        if (raw is null) return null;
        if (!TryParseSpec(raw, out var taskName, out var description)) return null;

        var spec = await _repo.GetOrCreateSpec(sessionId, moduleName, "");
        var task = await _repo.GetOrCreateTask(spec.Id, taskName, description);
        return await _repo.SaveVersion(task.Id, raw);
    }

    public async Task<List<SpecTaskVersion>> TryExtractSplitAndSave(Guid sessionId, string response, IClaudeClient claude, string moduleName = "General")
    {
        try
        {
            var raw = ExtractJsonRaw(response);
            if (raw is null) return new List<SpecTaskVersion>();

            // BA returned array directly
            if (raw.TrimStart().StartsWith("["))
            {
                var items = ExtractJsonArray(raw);
                if (items is null || items.Count == 0) return new List<SpecTaskVersion>();

                var spec = await _repo.GetOrCreateSpec(sessionId, moduleName, "");
                var saved = new List<SpecTaskVersion>();
                foreach (var item in items)
                {
                    if (!TryParseSpec(item, out var taskName, out var desc)) continue;
                    var task = await _repo.GetOrCreateTask(spec.Id, taskName, desc);
                    saved.Add(await _repo.SaveVersion(task.Id, item));
                }
                return saved;
            }

            if (!raw.Contains("feature_name")) return new List<SpecTaskVersion>();

            // Call splitter
            var splitResult = await claude.ChatWithPrompt(SplitterPrompt, raw);
            var array = ExtractJsonArray(splitResult.Text);
            if (array is null || array.Count == 0) return new List<SpecTaskVersion>();

            var specModule = await _repo.GetOrCreateSpec(sessionId, moduleName, "");
            var savedSplit = new List<SpecTaskVersion>();
            foreach (var item in array)
            {
                if (!TryParseSpec(item, out var taskName, out var desc)) continue;
                var task = await _repo.GetOrCreateTask(specModule.Id, taskName, desc);
                savedSplit.Add(await _repo.SaveVersion(task.Id, item));
            }
            return savedSplit;
        }
        catch (Exception ex)
        {
            FileLogger.Error("SpecService.TryExtractSplitAndSave", ex);
            return new List<SpecTaskVersion>();
        }
    }

    public async Task TryMergeAndSave(Guid sessionId, SpecTaskVersion existing, string response)
    {
        try
        {
            var blocks = ExtractAllJsonBlocks(response);
            if (blocks.Count == 0) return;

            var moduleSpec = existing.Task.Spec;
            bool patchApplied = false;

            foreach (var json in blocks)
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (dict is null) continue;

                if (dict.ContainsKey("feature_name"))
                {
                    // Full spec for a screen (new or different) → save as new task version
                    if (TryParseSpec(json, out var screenName, out var desc))
                    {
                        var task = await _repo.GetOrCreateTask(moduleSpec.Id, screenName, desc);
                        await _repo.SaveVersion(task.Id, json);
                    }
                }
                else if (!patchApplied)
                {
                    // Patch block (no feature_name) → merge into existing spec
                    var existingDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existing.JsonSpec);
                    if (existingDict is null) continue;
                    foreach (var kv in dict) existingDict[kv.Key] = kv.Value;
                    var merged = JsonSerializer.Serialize(existingDict, new JsonSerializerOptions { WriteIndented = true });
                    await _repo.SaveVersion(existing.TaskId, merged);
                    patchApplied = true;
                }
            }
        }
        catch (Exception ex) { FileLogger.Error("SpecService.TryMergeAndSave", ex); }
    }

    public Task<SpecTaskVersion?> GetLatestVersion(Guid sessionId) => _repo.GetLatestVersionInSession(sessionId);
    public Task<SpecTaskVersion?> GetVersionById(Guid versionId) => _repo.GetVersionById(versionId);
    public Task<SpecTaskVersion?> GetVersionByNumber(Guid taskId, int versionNumber) => _repo.GetVersionByNumber(taskId, versionNumber);
    public Task<List<Spec>> GetAllSpecs(Guid sessionId) => _repo.GetAllSpecs(sessionId);
    public Task<SpecTaskVersion?> UpdateVersion(Guid versionId, string jsonSpec) => _repo.UpdateVersion(versionId, jsonSpec);
    public Task DeleteVersion(Guid versionId) => _repo.DeleteVersion(versionId);
    public Task DeleteTask(Guid taskId) => _repo.DeleteTask(taskId);
    public Task DeleteSpec(Guid specId) => _repo.DeleteSpec(specId);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? ExtractJsonRaw(string response)
    {
        var blocks = ExtractAllJsonBlocks(response);
        return blocks.FirstOrDefault();
    }

    /// <summary>Extract ALL ```json``` blocks from response, in order.</summary>
    private static List<string> ExtractAllJsonBlocks(string response)
    {
        var results = new List<string>();
        foreach (Match m in Regex.Matches(response, @"```(?:json)?\s*([\s\S]*?)\s*```"))
        {
            var raw = m.Groups[1].Value.Trim();
            if (IsValidJson(raw)) results.Add(raw);
        }
        // Fallback: no fenced blocks, try bare objects
        if (results.Count == 0)
        {
            var m = Regex.Match(response, @"\{[\s\S]*\}");
            if (m.Success && IsValidJson(m.Value)) results.Add(m.Value);
        }
        return results;
    }

    private static List<string>? ExtractJsonArray(string response)
    {
        var match = Regex.Match(response, @"\[[\s\S]*\]");
        if (!match.Success) return null;
        try
        {
            using var doc = JsonDocument.Parse(match.Value);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;
            return doc.RootElement.EnumerateArray()
                .Select(el => JsonSerializer.Serialize(el, new JsonSerializerOptions { WriteIndented = true }))
                .ToList();
        }
        catch { return null; }
    }

    private static bool IsValidJson(string s)
    {
        try { JsonDocument.Parse(s); return true; }
        catch { return false; }
    }

    private static bool TryParseSpec(string json, out string name, out string description)
    {
        name = string.Empty; description = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("feature_name", out var n)) name = n.GetString() ?? string.Empty;
            if (root.TryGetProperty("description", out var d)) description = d.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(name);
        }
        catch { return false; }
    }

    private static string LoadPrompt(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Prompts", fileName);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
