using AgentCore.CodeMap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentService.Controllers;

[ApiController]
[Route("api/codemap")]
[Authorize]
public class CodeMapController : ControllerBase
{
    private readonly ICodeMapService _service;

    public CodeMapController(ICodeMapService service) { _service = service; }

    // ── Scan ──

    [HttpPost("{projectId:guid}/scan")]
    public async Task<IActionResult> Scan(Guid projectId, [FromBody] ScanRequest rq)
    {
        try
        {
            var stats = await _service.ScanProjectAsync(projectId, rq.RootPath);
            return Ok(stats);
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost("{projectId:guid}/scan/incremental")]
    public async Task<IActionResult> ScanIncremental(Guid projectId, [FromBody] ScanRequest rq)
    {
        try
        {
            var stats = await _service.ScanIncrementalAsync(projectId, rq.RootPath);
            return Ok(stats);
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost("{projectId:guid}/scan/add-path")]
    public async Task<IActionResult> ScanAddPath(Guid projectId, [FromBody] ScanRequest rq)
    {
        try
        {
            var stats = await _service.ScanAddPathAsync(projectId, rq.RootPath);
            return Ok(stats);
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace }); }
    }

    [HttpGet("{projectId:guid}/scan/progress")]
    public IActionResult GetScanProgress(Guid projectId)
    {
        var progress = _service.GetScanProgress(projectId);
        return Ok(progress ?? new ScanProgress { Phase = "idle" });
    }

    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> Clear(Guid projectId)
    {
        await _service.ClearProjectAsync(projectId);
        return Ok(new { message = "Cleared" });
    }

    // ── Queries ──

    [HttpGet("{projectId:guid}/symbols")]
    public async Task<IActionResult> FindSymbol(Guid projectId, [FromQuery] string name, [FromQuery] string? type = null)
        => Ok(await _service.FindSymbolAsync(projectId, name, type));

    [HttpGet("{projectId:guid}/endpoints")]
    public async Task<IActionResult> FindEndpoint(Guid projectId, [FromQuery] string? route = null, [FromQuery] string? method = null)
        => Ok(await _service.FindEndpointAsync(projectId, route, method));

    [HttpGet("{projectId:guid}/dependencies")]
    public async Task<IActionResult> GetDependencies(Guid projectId, [FromQuery] string symbol, [FromQuery] int depth = 1)
        => Ok(await _service.GetDependenciesAsync(projectId, symbol, depth));

    [HttpGet("{projectId:guid}/search")]
    public async Task<IActionResult> Search(Guid projectId, [FromQuery] string q, [FromQuery] int limit = 20)
        => Ok(await _service.SearchAsync(projectId, q, limit));

    // ── Stats & Tree (for Web UI) ──

    [HttpGet("{projectId:guid}/stats")]
    public async Task<IActionResult> GetStats(Guid projectId)
        => Ok(await _service.GetStatsAsync(projectId));

    [HttpGet("{projectId:guid}/tree")]
    public async Task<IActionResult> GetTree(Guid projectId)
        => Ok(await _service.GetTreeAsync(projectId));

    [HttpGet("{projectId:guid}/graph")]
    public async Task<IActionResult> GetGraph(Guid projectId, [FromQuery] string? filter = null)
        => Ok(await _service.GetGraphAsync(projectId, filter));

    [HttpGet("{projectId:guid}/graph/modules")]
    public async Task<IActionResult> GetModuleGraph(Guid projectId)
        => Ok(await _service.GetModuleGraphAsync(projectId));
}

public class ScanRequest
{
    public string RootPath { get; set; } = string.Empty;
}
