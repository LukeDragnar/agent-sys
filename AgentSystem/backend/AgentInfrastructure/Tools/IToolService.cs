using System.Text.Json;
using AgentInfrastructure.Clients;

namespace AgentInfrastructure.Tools;

public interface IToolService
{
    List<ToolDefinition> GetDefinitions();
    Task<string> Execute(string toolName, JsonElement input);
}
