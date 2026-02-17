using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PraxisNote.Application.Features.ActionItems;
using PraxisNote.Web.Mcp;

namespace PraxisNote.Web.Mcp.Tools;

[McpServerToolType]
public sealed class ActionItemTools(McpUserContext userContext)
{
    [McpServerTool, Description("Get outstanding (uncompleted) action items from recent meetings, including their linked task status.")]
    public async Task<string> GetOutstandingActionItems(GetOutstandingActionItems getOutstandingActionItems)
    {
        var query = new GetOutstandingActionItems.Query(userContext.UserId, userContext.ProfileId);
        var items = await getOutstandingActionItems.ExecuteAsync(query);
        return JsonSerializer.Serialize(items);
    }
}
