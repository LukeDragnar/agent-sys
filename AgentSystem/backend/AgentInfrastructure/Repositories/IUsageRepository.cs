using AgentInfrastructure.Entities;

namespace AgentInfrastructure.Repositories;

public interface IUsageRepository
{
    Task<int> GetWeeklyTokens(Guid userId, int week, int year);
    Task Log(Guid userId, Guid sessionId, int inputTokens, int outputTokens, int week, int year, string gitUser = "");
    Task<List<UsageLog>> GetUserLogs(Guid userId, DateTime? from, DateTime? to);
    Task<List<UserUsageSummary>> GetTopUsers(int week, int year, int topN);
    Task<List<DailyTrend>> GetDailyTrend(int days);
    Task<List<SessionTokenSummary>> GetSessionTokens(int week, int year, int topN);
    Task<TeamBudgetSummary> GetTeamSummary(int week, int year);
    Task<List<GitUserUsageSummary>> GetTopGitUsers(int week, int year, int topN);
    Task<int> GetWeeklyTokensByGitUser(string gitUser, int week, int year);

    // Date-range queries for Token Budget Dashboard
    Task<List<GitUserUsageSummary>> GetGitUsersByDateRange(DateTime from, DateTime to);
    Task<List<DailyTrend>> GetDailyTrendByRange(DateTime from, DateTime to);
    Task<List<SessionTokenSummary>> GetSessionTokensByRange(DateTime from, DateTime to, int topN);
    Task<DashboardTotals> GetTotalsByRange(DateTime from, DateTime to);
}

public class UserUsageSummary
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public int WeeklyLimit { get; set; }
    public double UsagePercent { get; set; }
}

public class DailyTrend
{
    public DateTime Date { get; set; }
    public int TotalTokens { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int ConversationCount { get; set; }
}

public class SessionTokenSummary
{
    public Guid SessionId { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string GitUser { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public int TotalTokens { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int MessageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TeamBudgetSummary
{
    public int TotalBudget { get; set; }
    public int TotalUsed { get; set; }
    public double UsagePercent { get; set; }
    public int ActiveUsers { get; set; }
    public int OverLimitUsers { get; set; }
}

public class GitUserUsageSummary
{
    public string GitUser { get; set; } = string.Empty;
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public int SessionCount { get; set; }
}

public class DashboardTotals
{
    public int TotalTokens { get; set; }
    public int TotalInput { get; set; }
    public int TotalOutput { get; set; }
    public int SessionCount { get; set; }
    public int GitUserCount { get; set; }
}
