using System.Globalization;
using AgentInfrastructure.Repositories;

namespace AgentCore.Services;

public class UsageService : IUsageService
{
    private readonly IUsageRepository _usage;
    private readonly IUserRepository _users;

    public UsageService(IUsageRepository usage, IUserRepository users)
    {
        _usage = usage;
        _users = users;
    }

    public async Task<bool> IsOverWeeklyLimit(Guid userId)
    {
        var user = await _users.GetById(userId);
        if (user is null || !user.IsActive) return true;
        if (user.WeeklyTokenLimit <= 0) return false; // 0 = unlimited
        var (week, year) = GetCurrentWeek();
        var used = await _usage.GetWeeklyTokens(userId, week, year);
        return used >= user.WeeklyTokenLimit;
    }

    public async Task LogUsage(Guid userId, Guid sessionId, int inputTokens, int outputTokens, string gitUser = "")
    {
        var (week, year) = GetCurrentWeek();
        await _usage.Log(userId, sessionId, inputTokens, outputTokens, week, year, gitUser);
    }

    public async Task<int> GetWeeklyTokensUsed(Guid userId)
    {
        var (week, year) = GetCurrentWeek();
        return await _usage.GetWeeklyTokens(userId, week, year);
    }

    public async Task<TokenBudgetDashboard> GetTokenBudget(DateTime from, DateTime to)
    {
        var totals = await _usage.GetTotalsByRange(from, to);
        var users = await _usage.GetGitUsersByDateRange(from, to);
        var dailyTrend = await _usage.GetDailyTrendByRange(from, to);
        var topSessions = await _usage.GetSessionTokensByRange(from, to, 50);

        return new TokenBudgetDashboard
        {
            Totals = totals,
            Users = users,
            DailyTrend = dailyTrend,
            TopSessions = topSessions,
            From = from,
            To = to
        };
    }

    private static (int week, int year) GetCurrentWeek()
    {
        var now = DateTime.UtcNow;
        return (ISOWeek.GetWeekOfYear(now), ISOWeek.GetYear(now));
    }
}
