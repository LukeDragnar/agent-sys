using System.Globalization;
using AgentCore.Services;
using AgentInfrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentService.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IUsageRepository _usage;
    private readonly IAgentService _agent;

    public AdminController(IUserRepository users, IUsageRepository usage, IAgentService agent)
    {
        _users = users;
        _usage = usage;
        _agent = agent;
    }

    // ── Users ─────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _users.GetAll();
        return Ok(users.Select(u => new
        {
            u.Id, u.Username, u.Role, u.WeeklyTokenLimit, u.IsActive, u.CreatedAt
        }));
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest rq)
    {
        var existing = await _users.GetByUsername(rq.Username);
        if (existing is not null)
            return Conflict(new { message = "Username already exists." });

        var hash = AuthService.HashPassword(rq.Password);
        var user = await _users.Create(rq.Username, hash, rq.Role, rq.WeeklyTokenLimit);
        return Ok(new { user.Id, user.Username, user.Role, user.WeeklyTokenLimit, user.IsActive, user.CreatedAt });
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest rq)
    {
        var user = await _users.GetById(id);
        if (user is null) return NotFound();

        if (!string.IsNullOrEmpty(rq.Password))
            user.PasswordHash = AuthService.HashPassword(rq.Password);
        if (!string.IsNullOrEmpty(rq.Role))
            user.Role = rq.Role;
        if (rq.WeeklyTokenLimit.HasValue)
            user.WeeklyTokenLimit = rq.WeeklyTokenLimit.Value;
        if (rq.IsActive.HasValue)
            user.IsActive = rq.IsActive.Value;

        await _users.Update(user);
        return Ok(new { user.Id, user.Username, user.Role, user.WeeklyTokenLimit, user.IsActive });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _users.Delete(id);
        return NoContent();
    }

    // ── Sessions ──────────────────────────────────────────────

    [HttpGet("sessions")]
    public async Task<IActionResult> GetAllSessions()
    {
        var sessions = await _agent.GetAllSessions();
        return Ok(sessions);
    }

    // ── Usage logs ────────────────────────────────────────────

    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage(
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        if (userId is null) return BadRequest("userId is required.");
        var logs = await _usage.GetUserLogs(userId.Value, from, to);
        return Ok(logs);
    }

    // ── Reports ───────────────────────────────────────────────

    [HttpGet("reports/top-users")]
    public async Task<IActionResult> GetTopUsers(
        [FromQuery] int? week,
        [FromQuery] int? year,
        [FromQuery] int top = 20)
    {
        var now = DateTime.UtcNow;
        var w = week ?? ISOWeek.GetWeekOfYear(now);
        var y = year ?? ISOWeek.GetYear(now);
        var result = await _usage.GetTopUsers(w, y, top);
        return Ok(new { week = w, year = y, data = result });
    }

    [HttpGet("reports/weekly-summary")]
    public async Task<IActionResult> WeeklySummary([FromQuery] Guid userId,
        [FromQuery] int weeks = 8)
    {
        var now = DateTime.UtcNow;
        var result = new List<object>();
        for (var i = 0; i < weeks; i++)
        {
            var date = now.AddDays(-7 * i);
            var w = ISOWeek.GetWeekOfYear(date);
            var y = ISOWeek.GetYear(date);
            var tokens = await _usage.GetWeeklyTokens(userId, w, y);
            result.Add(new { week = w, year = y, totalTokens = tokens });
        }
        return Ok(result);
    }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public int WeeklyTokenLimit { get; set; } = 100_000;
}

public class UpdateUserRequest
{
    public string? Password { get; set; }
    public string? Role { get; set; }
    public int? WeeklyTokenLimit { get; set; }
    public bool? IsActive { get; set; }
}
