namespace AgentInfrastructure.Entities;

public class UsageLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int IsoWeek { get; set; }
    public int IsoYear { get; set; }
    public string GitUser { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public AppUser User { get; set; } = null!;
    public AgentSession Session { get; set; } = null!;
}
