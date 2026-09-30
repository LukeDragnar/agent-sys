using System.Security.Claims;
using AgentCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentService.Controllers;

[ApiController]
[Route("api/orchestrator")]
[Authorize]
public class OrchestratorController : ControllerBase
{
    private readonly IOrchestratorService _service;

    public OrchestratorController(IOrchestratorService service) { _service = service; }

    private Guid? CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] RunRequest rq)
    {
        try
        {
            var result = await _service.RunAsync(rq.FeatureRequest);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("run-from-spec-split")]
    public async Task<IActionResult> RunFromSpecSplit([FromBody] RunFromSpecRequest rq)
    {
        try
        {
            var result = await _service.RunFromSpecSplitAsync(rq.SpecJson);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("run-from-spec")]
    public async Task<IActionResult> RunFromSpec([FromBody] RunFromSpecRequest rq)
    {
        try
        {
            var result = await _service.RunFromSpecAsync(rq.SpecJson);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("run-from-version")]
    public async Task<IActionResult> RunFromVersion([FromBody] RunFromVersionRequest rq)
    {
        try
        {
            var result = await _service.RunFromVersionAsync(rq.VersionId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("run-from-action")]
    public async Task<IActionResult> RunFromAction([FromBody] RunFromActionRequest rq)
    {
        try
        {
            var result = await _service.RunFromActionAsync(rq.VersionId, rq.ActionName);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // Async job-based endpoints — return jobId immediately, pipeline runs in background
    [HttpPost("start-from-version")]
    public async Task<IActionResult> StartFromVersion([FromBody] RunFromVersionRequest rq)
    {
        try
        {
            var jobId = await _service.StartRunFromVersion(rq.VersionId, rq.OutputDir, rq.ProjectId, CurrentUserId, rq.SelectedModules);
            return Ok(new { jobId });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("start-from-action")]
    public async Task<IActionResult> StartFromAction([FromBody] RunFromActionRequest rq)
    {
        try
        {
            var jobId = await _service.StartRunFromAction(rq.VersionId, rq.ActionName, rq.OutputDir, rq.ProjectId, CurrentUserId, rq.SelectedModules);
            return Ok(new { jobId });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("job/{jobId}")]
    public IActionResult GetJob(Guid jobId)
    {
        var job = _service.GetJob(jobId);
        if (job == null) return NotFound();
        return Ok(new
        {
            logs = job.Logs,
            isComplete = job.IsComplete,
            result = job.Result,
            error = job.Error
        });
    }

    [HttpGet("version/{versionId}/files")]
    public IActionResult GetFilesForVersion(Guid versionId)
    {
        var files = _service.GetFilesForVersion(versionId);
        if (files == null) return Ok(Array.Empty<object>());
        return Ok(files);
    }

    [HttpPost("scan-project")]
    public async Task<IActionResult> ScanProject([FromBody] ScanProjectRequest rq)
    {
        try
        {
            var context = await _service.ScanProjectAsync(rq.BePath, rq.FePath);
            return Ok(new { context });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("project-context")]
    public IActionResult GetProjectContext()
        => Ok(new { context = _service.GetProjectContext() });

    [HttpPost("project-context")]
    public IActionResult SetProjectContext([FromBody] SetContextRequest rq)
    {
        _service.SetProjectContext(rq.Context);
        return Ok();
    }

    [HttpPost("list-dir")]
    public IActionResult ListDir([FromBody] PathRequest rq)
    {
        try { return Ok(_service.ListDir(rq.Path)); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("read-file")]
    public async Task<IActionResult> ReadFile([FromBody] PathRequest rq)
    {
        try
        {
            var content = await _service.ReadFileAsync(rq.Path);
            return Ok(new { content });
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }
}

public class RunRequest
{
    public string FeatureRequest { get; set; } = string.Empty;
}

public class RunFromSpecRequest
{
    public string SpecJson { get; set; } = string.Empty;
}

public class RunFromVersionRequest
{
    public Guid VersionId { get; set; }
    public string? OutputDir { get; set; }
    public Guid? ProjectId { get; set; }
    public List<string>? SelectedModules { get; set; }
}

public class RunFromActionRequest
{
    public Guid VersionId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string? OutputDir { get; set; }
    public Guid? ProjectId { get; set; }
    public List<string>? SelectedModules { get; set; }
}

public class ScanProjectRequest
{
    public string BePath { get; set; } = string.Empty;
    public string FePath { get; set; } = string.Empty;
}

public class SetContextRequest
{
    public string Context { get; set; } = string.Empty;
}

public class PathRequest
{
    public string Path { get; set; } = string.Empty;
}
