using System.Security.Claims;
using AgentCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentService.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _service;

    public ProjectController(IProjectService service) { _service = service; }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAll());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var p = await _service.GetById(id);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest rq)
    {
        var p = await _service.Create(rq.Name, rq.Description ?? "", CurrentUserId);
        return Ok(p);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateProjectRequest rq)
    {
        var p = await _service.Update(id, rq.Name, rq.Description ?? "");
        return p == null ? NotFound() : Ok(p);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.Delete(id);
        return Ok();
    }

    [HttpPost("{id:guid}/scan")]
    public async Task<IActionResult> Scan(Guid id, [FromBody] ScanProjectPathsRequest rq)
    {
        try
        {
            var profile = await _service.ScanAsync(id, rq.BePath, rq.FePath, CurrentUserId);
            return Ok(new { profile });
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpGet("{id:guid}/my-config")]
    public async Task<IActionResult> GetMyConfig(Guid id)
    {
        var config = await _service.GetUserConfig(id, CurrentUserId);
        return Ok(config);
    }

    [HttpPut("{id:guid}/my-config")]
    public async Task<IActionResult> UpsertMyConfig(Guid id, [FromBody] UserConfigRequest rq)
    {
        var config = await _service.UpsertUserConfig(id, CurrentUserId, rq.LocalBePath, rq.LocalFePath, rq.LocalOutputDir, rq.LocalModulesJson);
        return Ok(config);
    }
}

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ScanProjectPathsRequest
{
    public string BePath { get; set; } = string.Empty;
    public string FePath { get; set; } = string.Empty;
}

public class UserConfigRequest
{
    public string LocalBePath { get; set; } = string.Empty;
    public string LocalFePath { get; set; } = string.Empty;
    public string LocalOutputDir { get; set; } = string.Empty;
    public string? LocalModulesJson { get; set; }
}
