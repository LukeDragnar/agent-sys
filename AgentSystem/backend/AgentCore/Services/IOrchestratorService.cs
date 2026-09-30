namespace AgentCore.Services;

public interface IOrchestratorService
{
    Task<OrchestratorResult> RunAsync(string featureRequest);
    Task<OrchestratorResult> RunFromSpecAsync(string specJson);
    Task<OrchestratorResult> RunFromSpecSplitAsync(string specJson);
    Task<OrchestratorResult> RunFromVersionAsync(Guid versionId);
    Task<OrchestratorResult> RunFromActionAsync(Guid versionId, string actionName);

    // Async job-based: fetches spec in current scope, then runs pipeline in background
    Task<Guid> StartRunFromVersion(Guid versionId, string? outputDir = null, Guid? projectId = null, Guid? userId = null, List<string>? selectedModules = null);
    Task<Guid> StartRunFromAction(Guid versionId, string actionName, string? outputDir = null, Guid? projectId = null, Guid? userId = null, List<string>? selectedModules = null);
    OrchestratorJobState? GetJob(Guid jobId);

    // Project context (scan existing code for conventions)
    Task<string> ScanProjectAsync(string bePath, string fePath);
    string? GetProjectContext();
    void SetProjectContext(string context);

    // Files generated for a specific spec version
    List<GeneratedFile>? GetFilesForVersion(Guid versionId);

    // Lazy file browser: list immediate children of a directory
    List<DirEntry> ListDir(string directoryPath);

    // Read a single file's content
    Task<string> ReadFileAsync(string filePath);
}

public class DirEntry
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
}

public class OrchestratorJobState
{
    private readonly List<string> _logs = new();
    public IReadOnlyList<string> Logs => _logs;
    public bool IsComplete { get; set; }
    public OrchestratorResult? Result { get; set; }
    public string? Error { get; set; }

    public void AddLog(string message)
    {
        lock (_logs) _logs.Add($"[{DateTime.UtcNow:HH:mm:ss}] {message}");
    }
}

public class OrchestratorResult
{
    public string Spec { get; set; } = string.Empty;
    public List<GeneratedFile> Files { get; set; } = new();
    public string QaReport { get; set; } = string.Empty;
    public string GitInstructions { get; set; } = string.Empty;
    public string Status { get; set; } = "PASS";
    public int RetryCount { get; set; }
    public string? Error { get; set; }
    public string OutputDir { get; set; } = string.Empty;
    public List<SubTaskResult>? SubResults { get; set; }
}

public class SubTaskResult
{
    public string SpecName { get; set; } = string.Empty;
    public string Spec { get; set; } = string.Empty;
    public List<GeneratedFile> Files { get; set; } = new();
    public string QaReport { get; set; } = string.Empty;
    public string Status { get; set; } = "PASS";
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}

public class GeneratedFile
{
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
