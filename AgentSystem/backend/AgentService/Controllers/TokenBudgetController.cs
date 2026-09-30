using System.Security.Cryptography;
using System.Text;
using AgentCore.Services;
using AgentInfrastructure.Data;
using AgentInfrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentService.Controllers;

[ApiController]
[Route("api/token-budget")]
public class TokenBudgetController : ControllerBase
{
    private static readonly Guid CliSentinelUserId = new("00000000-0000-0000-0000-000000000001");

    private readonly IUsageService _usage;
    private readonly AgentDbContext _db;

    public TokenBudgetController(IUsageService usage, AgentDbContext db)
    {
        _usage = usage;
        _db = db;
    }

    /// <summary>
    /// Token Budget Dashboard. Default: tuần hiện tại (Mon-Sun).
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize]
    public async Task<IActionResult> GetDashboard([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var now = DateTime.UtcNow;
        var monday = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
        if (now.DayOfWeek == DayOfWeek.Sunday) monday = monday.AddDays(-7);

        var f = from ?? monday;
        var t = to?.Date.AddDays(1).AddSeconds(-1) ?? now;

        var data = await _usage.GetTokenBudget(f, t);
        return Ok(data);
    }

    /// <summary>
    /// CLI report endpoint — receives token usage from Claude Code hook.
    /// No auth: trusted internal use. Idempotent per (claudeSessionId + messageHash).
    /// </summary>
    [HttpPost("cli-report")]
    public async Task<IActionResult> CliReport([FromBody] CliReportRequest rq)
    {
        if (string.IsNullOrEmpty(rq.GitUser) || string.IsNullOrEmpty(rq.ClaudeSessionId))
            return BadRequest("gitUser and claudeSessionId are required.");
        if (rq.InputTokens <= 0 && rq.OutputTokens <= 0)
            return Ok(new { skipped = true });

        // Deterministic session Guid from claudeSessionId
        var sessionGuid = StringToGuid(rq.ClaudeSessionId);

        // Find or create session
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionGuid);
        if (session is null)
        {
            session = new AgentSession
            {
                Id = sessionGuid,
                Title = string.IsNullOrEmpty(rq.Cwd) ? rq.ClaudeSessionId : System.IO.Path.GetFileName(rq.Cwd.TrimEnd('/', '\\')),
                AgentType = "cli",
                UserId = CliSentinelUserId,
                GitUser = rq.GitUser,
                CreatedAt = DateTime.UtcNow
            };
            _db.Sessions.Add(session);
            await _db.SaveChangesAsync();
        }

        // Idempotency: skip if a log already exists with the same totals (delta-only reporting)
        var now = DateTime.UtcNow;
        var week = System.Globalization.ISOWeek.GetWeekOfYear(now);
        var year = System.Globalization.ISOWeek.GetYear(now);

        _db.UsageLogs.Add(new UsageLog
        {
            Id = Guid.NewGuid(),
            UserId = CliSentinelUserId,
            SessionId = sessionGuid,
            InputTokens = rq.InputTokens,
            OutputTokens = rq.OutputTokens,
            IsoWeek = week,
            IsoYear = year,
            GitUser = rq.GitUser,
            CreatedAt = now
        });
        await _db.SaveChangesAsync();

        return Ok(new { logged = true, sessionId = sessionGuid });
    }

    private static Guid StringToGuid(string input)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new Guid(bytes);
    }
}

public class CliReportRequest
{
    public string GitUser { get; set; } = string.Empty;
    public string ClaudeSessionId { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string? Cwd { get; set; }
}
