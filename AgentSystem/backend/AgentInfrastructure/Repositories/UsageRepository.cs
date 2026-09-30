using AgentInfrastructure.Data;
using AgentInfrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentInfrastructure.Repositories;

public class UsageRepository : IUsageRepository
{
    private readonly AgentDbContext _db;

    public UsageRepository(AgentDbContext db) => _db = db;

    public async Task<int> GetWeeklyTokens(Guid userId, int week, int year) =>
        await _db.UsageLogs
            .Where(x => x.UserId == userId && x.IsoWeek == week && x.IsoYear == year)
            .SumAsync(x => x.InputTokens + x.OutputTokens);

    public async Task Log(Guid userId, Guid sessionId, int inputTokens, int outputTokens, int week, int year, string gitUser = "")
    {
        _db.UsageLogs.Add(new UsageLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            IsoWeek = week,
            IsoYear = year,
            GitUser = gitUser,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<UsageLog>> GetUserLogs(Guid userId, DateTime? from, DateTime? to)
    {
        var q = _db.UsageLogs.Where(x => x.UserId == userId);
        if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);
        return await q.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    public async Task<List<UserUsageSummary>> GetTopUsers(int week, int year, int topN)
    {
        var grouped = await _db.UsageLogs
            .Where(x => x.IsoWeek == week && x.IsoYear == year)
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalIn = g.Sum(x => x.InputTokens),
                TotalOut = g.Sum(x => x.OutputTokens),
                Total = g.Sum(x => x.InputTokens + x.OutputTokens)
            })
            .OrderByDescending(x => x.Total)
            .Take(topN)
            .ToListAsync();

        var ids = grouped.Select(x => x.UserId).ToList();
        var users = await _db.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        return grouped.Select(x =>
        {
            users.TryGetValue(x.UserId, out var u);
            return new UserUsageSummary
            {
                UserId = x.UserId,
                Username = u?.Username ?? "?",
                TotalInputTokens = x.TotalIn,
                TotalOutputTokens = x.TotalOut,
                TotalTokens = x.Total,
                WeeklyLimit = u?.WeeklyTokenLimit ?? 0,
                UsagePercent = (u?.WeeklyTokenLimit ?? 0) > 0
                    ? Math.Round((double)x.Total / u!.WeeklyTokenLimit * 100, 1)
                    : 0
            };
        }).ToList();
    }

    public async Task<List<DailyTrend>> GetDailyTrend(int days)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days + 1);
        var raw = await _db.UsageLogs
            .Where(x => x.CreatedAt >= since)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new DailyTrend
            {
                Date = g.Key,
                InputTokens = g.Sum(x => x.InputTokens),
                OutputTokens = g.Sum(x => x.OutputTokens),
                TotalTokens = g.Sum(x => x.InputTokens + x.OutputTokens),
                ConversationCount = g.Select(x => x.SessionId).Distinct().Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Fill missing days with zeros
        var result = new List<DailyTrend>();
        for (var d = since; d <= DateTime.UtcNow.Date; d = d.AddDays(1))
        {
            var existing = raw.FirstOrDefault(x => x.Date == d);
            result.Add(existing ?? new DailyTrend { Date = d });
        }
        return result;
    }

    public async Task<List<SessionTokenSummary>> GetSessionTokens(int week, int year, int topN)
    {
        var grouped = await _db.UsageLogs
            .Where(x => x.IsoWeek == week && x.IsoYear == year)
            .GroupBy(x => x.SessionId)
            .Select(g => new
            {
                SessionId = g.Key,
                InputTokens = g.Sum(x => x.InputTokens),
                OutputTokens = g.Sum(x => x.OutputTokens),
                TotalTokens = g.Sum(x => x.InputTokens + x.OutputTokens),
                MessageCount = g.Count(),
                UserId = g.First().UserId
            })
            .OrderByDescending(x => x.TotalTokens)
            .Take(topN)
            .ToListAsync();

        var sessionIds = grouped.Select(x => x.SessionId).ToList();
        var sessions = await _db.Sessions
            .Where(s => sessionIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        var userIds = grouped.Select(x => x.UserId).Distinct().ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        return grouped.Select(x =>
        {
            sessions.TryGetValue(x.SessionId, out var s);
            users.TryGetValue(x.UserId, out var u);
            return new SessionTokenSummary
            {
                SessionId = x.SessionId,
                SessionTitle = s?.Title ?? "?",
                Username = u?.Username ?? "?",
                GitUser = s?.GitUser ?? "",
                AgentType = s?.AgentType ?? "?",
                TotalTokens = x.TotalTokens,
                InputTokens = x.InputTokens,
                OutputTokens = x.OutputTokens,
                MessageCount = x.MessageCount,
                CreatedAt = s?.CreatedAt ?? DateTime.MinValue
            };
        }).ToList();
    }

    public async Task<TeamBudgetSummary> GetTeamSummary(int week, int year)
    {
        var activeUsers = await _db.Users.Where(u => u.IsActive).ToListAsync();
        var totalBudget = activeUsers.Sum(u => u.WeeklyTokenLimit);

        var weeklyUsage = await _db.UsageLogs
            .Where(x => x.IsoWeek == week && x.IsoYear == year)
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.InputTokens + x.OutputTokens) })
            .ToListAsync();

        var totalUsed = weeklyUsage.Sum(x => x.Total);
        var overLimit = weeklyUsage.Count(x =>
        {
            var user = activeUsers.FirstOrDefault(u => u.Id == x.UserId);
            return user is { WeeklyTokenLimit: > 0 } && x.Total >= user.WeeklyTokenLimit;
        });

        return new TeamBudgetSummary
        {
            TotalBudget = totalBudget,
            TotalUsed = totalUsed,
            UsagePercent = totalBudget > 0 ? Math.Round((double)totalUsed / totalBudget * 100, 1) : 0,
            ActiveUsers = activeUsers.Count,
            OverLimitUsers = overLimit
        };
    }

    public async Task<List<GitUserUsageSummary>> GetTopGitUsers(int week, int year, int topN)
    {
        return await _db.UsageLogs
            .Where(x => x.IsoWeek == week && x.IsoYear == year && x.GitUser != "")
            .GroupBy(x => x.GitUser)
            .Select(g => new GitUserUsageSummary
            {
                GitUser = g.Key,
                TotalInputTokens = g.Sum(x => x.InputTokens),
                TotalOutputTokens = g.Sum(x => x.OutputTokens),
                TotalTokens = g.Sum(x => x.InputTokens + x.OutputTokens),
                SessionCount = g.Select(x => x.SessionId).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalTokens)
            .Take(topN)
            .ToListAsync();
    }

    public async Task<int> GetWeeklyTokensByGitUser(string gitUser, int week, int year) =>
        await _db.UsageLogs
            .Where(x => x.GitUser == gitUser && x.IsoWeek == week && x.IsoYear == year)
            .SumAsync(x => x.InputTokens + x.OutputTokens);

    public async Task<List<GitUserUsageSummary>> GetGitUsersByDateRange(DateTime from, DateTime to)
    {
        return await _db.UsageLogs
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .GroupBy(x => x.GitUser)
            .Select(g => new GitUserUsageSummary
            {
                GitUser = g.Key,
                TotalInputTokens = g.Sum(x => x.InputTokens),
                TotalOutputTokens = g.Sum(x => x.OutputTokens),
                TotalTokens = g.Sum(x => x.InputTokens + x.OutputTokens),
                SessionCount = g.Select(x => x.SessionId).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalTokens)
            .ToListAsync();
    }

    public async Task<List<DailyTrend>> GetDailyTrendByRange(DateTime from, DateTime to)
    {
        var raw = await _db.UsageLogs
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new DailyTrend
            {
                Date = g.Key,
                InputTokens = g.Sum(x => x.InputTokens),
                OutputTokens = g.Sum(x => x.OutputTokens),
                TotalTokens = g.Sum(x => x.InputTokens + x.OutputTokens),
                ConversationCount = g.Select(x => x.SessionId).Distinct().Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var result = new List<DailyTrend>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            var existing = raw.FirstOrDefault(x => x.Date == d);
            result.Add(existing ?? new DailyTrend { Date = d });
        }
        return result;
    }

    public async Task<List<SessionTokenSummary>> GetSessionTokensByRange(DateTime from, DateTime to, int topN)
    {
        var grouped = await _db.UsageLogs
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .GroupBy(x => x.SessionId)
            .Select(g => new
            {
                SessionId = g.Key,
                InputTokens = g.Sum(x => x.InputTokens),
                OutputTokens = g.Sum(x => x.OutputTokens),
                TotalTokens = g.Sum(x => x.InputTokens + x.OutputTokens),
                MessageCount = g.Count()
            })
            .OrderByDescending(x => x.TotalTokens)
            .Take(topN)
            .ToListAsync();

        var sessionIds = grouped.Select(x => x.SessionId).ToList();
        var sessions = await _db.Sessions
            .Where(s => sessionIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        return grouped.Select(x =>
        {
            sessions.TryGetValue(x.SessionId, out var s);
            return new SessionTokenSummary
            {
                SessionId = x.SessionId,
                SessionTitle = s?.Title ?? "?",
                Username = "",
                GitUser = s?.GitUser ?? "",
                AgentType = s?.AgentType ?? "?",
                TotalTokens = x.TotalTokens,
                InputTokens = x.InputTokens,
                OutputTokens = x.OutputTokens,
                MessageCount = x.MessageCount,
                CreatedAt = s?.CreatedAt ?? DateTime.MinValue
            };
        }).ToList();
    }

    public async Task<DashboardTotals> GetTotalsByRange(DateTime from, DateTime to)
    {
        var logs = _db.UsageLogs.Where(x => x.CreatedAt >= from && x.CreatedAt <= to);
        var totalInput = await logs.SumAsync(x => x.InputTokens);
        var totalOutput = await logs.SumAsync(x => x.OutputTokens);
        var sessionCount = await logs.Select(x => x.SessionId).Distinct().CountAsync();
        var gitUserCount = await logs.Select(x => x.GitUser).Distinct().CountAsync();

        return new DashboardTotals
        {
            TotalInput = totalInput,
            TotalOutput = totalOutput,
            TotalTokens = totalInput + totalOutput,
            SessionCount = sessionCount,
            GitUserCount = gitUserCount
        };
    }
}
