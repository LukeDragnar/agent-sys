namespace AgentCore.Services;

public interface IAgentLoader
{
    /// <summary>
    /// Build system prompt từ agent definition + rules + project context.
    /// agentType: tên file trong agents/ (không có .md), ví dụ "git-agent", "code-agent"
    /// projectKey: tên folder trong projects/, ví dụ "scrm". Null = không load project context.
    /// </summary>
    Task<string> BuildSystemPrompt(string agentType, string? projectKey = null);

    /// <summary>
    /// Load raw content của agent definition file.
    /// </summary>
    Task<string> LoadDefinition(string agentType);
}
