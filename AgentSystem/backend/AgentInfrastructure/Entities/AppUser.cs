namespace AgentInfrastructure.Entities;

public class AppUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "user"; // "user" | "admin"
    public int WeeklyTokenLimit { get; set; } = 100_000;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public List<UsageLog> UsageLogs { get; set; } = new();
}
