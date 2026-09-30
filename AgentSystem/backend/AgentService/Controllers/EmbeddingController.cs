using AgentCore.Embedding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentService.Controllers;

[ApiController]
[Route("api/embedding")]
[Authorize]
public class EmbeddingController : ControllerBase
{
    private readonly IEmbeddingService _service;

    public EmbeddingController(IEmbeddingService service) { _service = service; }

    [HttpPost("{projectId:guid}/index")]
    public async Task<IActionResult> Index(Guid projectId, [FromBody] EmbedIndexRequest rq)
    {
        try { return Ok(await _service.IndexProjectAsync(projectId, rq.RootPath)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost("{projectId:guid}/index/incremental")]
    public async Task<IActionResult> IndexIncremental(Guid projectId, [FromBody] EmbedIndexRequest rq)
    {
        try { return Ok(await _service.IndexIncrementalAsync(projectId, rq.RootPath)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpGet("{projectId:guid}/progress")]
    public IActionResult Progress(Guid projectId)
    {
        var p = _service.GetProgress(projectId);
        return Ok(p ?? new EmbedIndexProgress { Phase = "idle" });
    }

    [HttpGet("{projectId:guid}/search")]
    public async Task<IActionResult> Search(Guid projectId, [FromQuery] string q, [FromQuery] int limit = 10)
        => Ok(await _service.SearchAsync(projectId, q, limit));

    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> Clear(Guid projectId)
    {
        await _service.ClearProjectAsync(projectId);
        return Ok(new { message = "Cleared" });
    }
}

public class EmbedIndexRequest
{
    public string RootPath { get; set; } = string.Empty;
}
