using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PraxisNote.Application.Features.Goals;
using PraxisNote.Web.Mcp;

namespace PraxisNote.Web.Mcp.Tools;

[McpServerToolType]
public sealed class GoalTools(McpUserContext userContext)
{
    [McpServerTool, Description("List all behavioral goals for the current user.")]
    public async Task<string> ListGoals(GetUserGoals getUserGoals)
    {
        var query = new GetUserGoals.Query(userContext.UserId, userContext.ProfileId);
        var goals = await getUserGoals.ExecuteAsync(query);
        return JsonSerializer.Serialize(goals);
    }
}
