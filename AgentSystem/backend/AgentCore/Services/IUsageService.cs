using AgentInfrastructure.Repositories;

namespace AgentCore.Services;

public interface IUsageService
{
    Task<bool> IsOverWeeklyLimit(Guid userId);
    Task LogUsage(Guid userId, Guid sessionId, int inputTokens, int outputTokens, string gitUser = "");
    Task<int> GetWeeklyTokensUsed(Guid userId);
    Task<TokenBudgetDashboard> GetTokenBudget(DateTime from, DateTime to);
}

public class TokenBudgetDashboard
{
    public DashboardTotals Totals { get; set; } = null!;
    public List<GitUserUsageSummary> Users { get; set; } = new();
    public List<DailyTrend> DailyTrend { get; set; } = new();
    public List<SessionTokenSummary> TopSessions { get; set; } = new();
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}
