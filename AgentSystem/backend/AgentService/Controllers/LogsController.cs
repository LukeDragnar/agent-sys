using AgentInfrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentService.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly IOrchestratorLogRepository _repo;

    public LogsController(IOrchestratorLogRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? agentType = null)
    {
        var logs = await _repo.GetAll(agentType);
        return Ok(logs);
    }
}
