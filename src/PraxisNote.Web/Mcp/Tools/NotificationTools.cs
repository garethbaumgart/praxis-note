using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PraxisNote.Application.Features.Notifications;
using PraxisNote.Web.Mcp;

namespace PraxisNote.Web.Mcp.Tools;

[McpServerToolType]
public sealed class NotificationTools(McpUserContext userContext)
{
    [McpServerTool, Description("Get all feature notifications showing new features and updates, with seen/unseen status.")]
    public async Task<string> GetNotifications(GetNotifications getNotifications)
    {
        var query = new GetNotifications.Query(userContext.UserId);
        var notifications = await getNotifications.ExecuteAsync(query);
        return JsonSerializer.Serialize(notifications);
    }
}
