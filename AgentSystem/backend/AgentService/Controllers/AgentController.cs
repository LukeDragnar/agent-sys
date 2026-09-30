using System.Security.Claims;
using AgentCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentService.Controllers;

[ApiController]
[Route("api/agent")]
[Authorize]
public class AgentController : ControllerBase
{
    private readonly IAgentService _service;
    private readonly ISpecService _spec;

    public AgentController(IAgentService service, ISpecService spec)
    {
        _service = service;
        _spec = spec;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GitUser => Request.Headers.TryGetValue("X-Git-User", out var val) ? val.ToString() : "";

    [HttpPost("session")]
    public async Task<IActionResult> CreateSession([FromBody] string title)
    {
        var result = await _service.CreateSession(title, UserId, GitUser);
        return Ok(result);
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest rq)
    {
        try
        {
            var res = await _service.Chat(rq.SessionId, rq.Message, UserId, rq.SpecId, rq.ProjectId, rq.SelectedModules, rq.AgentType, rq.ProjectKey, GitUser);
            return Ok(res);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("limit"))
        {
            return StatusCode(429, ex.Message);
        }
    }

    [HttpPost("qa-spec")]
    public async Task<IActionResult> QaSpec([FromBody] QaSpecRequest rq)
    {
        var result = await _service.QaSpecAsync(rq.VersionId, UserId, rq.ProjectId, rq.SelectedModules);
        return Ok(result);
    }

    [HttpGet("history/{sessionId}")]
    public async Task<IActionResult> History(Guid sessionId)
    {
        var data = await _service.GetHistory(sessionId);
        return Ok(data);
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var sessions = await _service.GetSessionsByUser(UserId);
        return Ok(sessions);
    }

    [HttpGet("spec/{sessionId}/versions")]
    public async Task<IActionResult> GetSpecs(Guid sessionId)
    {
        var specs = await _spec.GetAllSpecs(sessionId);
        var result = specs.Select(s => new SpecDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            CreatedAt = s.CreatedAt,
            Tasks = s.Tasks.Select(t => new SpecTaskDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                Versions = t.Versions.OrderByDescending(v => v.Version).Select(v => new SpecVersionDto
                {
                    Id = v.Id,
                    Version = v.Version,
                    JsonSpec = v.JsonSpec,
                    CreatedAt = v.CreatedAt
                }).ToList()
            }).ToList()
        }).ToList();
        return Ok(result);
    }

    [HttpDelete("session/{sessionId}")]
    public async Task<IActionResult> DeleteSession(Guid sessionId)
    {
        await _service.DeleteSession(sessionId);
        return NoContent();
    }

    [HttpPatch("session/{sessionId}/rename")]
    public async Task<IActionResult> RenameSession(Guid sessionId, [FromBody] string newTitle)
    {
        await _service.RenameSession(sessionId, newTitle);
        return NoContent();
    }

    [HttpPatch("session/{sessionId}/favorite")]
    public async Task<IActionResult> SetFavorite(Guid sessionId, [FromBody] bool isFavorite)
    {
        await _service.SetFavorite(sessionId, isFavorite);
        return NoContent();
    }

    [HttpPatch("session/{sessionId}/archive")]
    public async Task<IActionResult> SetArchived(Guid sessionId, [FromBody] bool isArchived)
    {
        await _service.SetArchived(sessionId, isArchived);
        return NoContent();
    }

    [HttpPatch("version/{versionId}")]
    public async Task<IActionResult> UpdateVersion(Guid versionId, [FromBody] string jsonSpec)
    {
        var result = await _spec.UpdateVersion(versionId, jsonSpec);
        if (result is null) return NotFound();
        return Ok(new SpecVersionDto
        {
            Id = result.Id,
            Version = result.Version,
            JsonSpec = result.JsonSpec,
            CreatedAt = result.CreatedAt
        });
    }

    [HttpDelete("version/{versionId}")]
    public async Task<IActionResult> DeleteVersion(Guid versionId)
    {
        await _spec.DeleteVersion(versionId);
        return NoContent();
    }

    [HttpDelete("task/{taskId}")]
    public async Task<IActionResult> DeleteTask(Guid taskId)
    {
        await _spec.DeleteTask(taskId);
        return NoContent();
    }

    [HttpDelete("spec/{specId}")]
    public async Task<IActionResult> DeleteSpec(Guid specId)
    {
        await _spec.DeleteSpec(specId);
        return NoContent();
    }
}

public class ChatRequest
{
    public Guid SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? SpecId { get; set; }
    public Guid? ProjectId { get; set; }
    public List<string>? SelectedModules { get; set; }
    /// <summary>Tên agent định nghĩa trong agents/*.md. Null = dùng BA agent mặc định.</summary>
    public string? AgentType { get; set; }
    /// <summary>Tên project trong projects/. Dùng để load project context files.</summary>
    public string? ProjectKey { get; set; }
}

public class QaSpecRequest
{
    public Guid VersionId { get; set; }
    public Guid? ProjectId { get; set; }
    public List<string>? SelectedModules { get; set; }
}

public class SpecVersionDto
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string JsonSpec { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SpecTaskDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<SpecVersionDto> Versions { get; set; } = new();
}

public class SpecDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<SpecTaskDto> Tasks { get; set; } = new();
}
