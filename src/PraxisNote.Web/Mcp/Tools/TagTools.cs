using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PraxisNote.Application.Features.Tags;
using PraxisNote.Web.Mcp;

namespace PraxisNote.Web.Mcp.Tools;

[McpServerToolType]
public sealed class TagTools(McpUserContext userContext)
{
    [McpServerTool, Description("List all tags for the current user with usage counts across tasks, notes, and meetings.")]
    public async Task<string> ListTags(GetUserTags getUserTags)
    {
        try
        {
            var query = new GetUserTags.Query(userContext.UserId, userContext.ProfileId);
            var tags = await getUserTags.ExecuteAsync(query);
            return JsonSerializer.Serialize(tags);
        }
        catch (Exception ex)
        {
            return McpErrorHelper.Serialize(ex);
        }
    }

    [McpServerTool, Description("Create a new tag.")]
    public async Task<string> CreateTag(
        CreateTag createTag,
        [Description("The name of the tag to create")] string name)
    {
        try
        {
            var command = new CreateTag.Command(userContext.UserId, userContext.ProfileId, name);
            var result = await createTag.ExecuteAsync(command);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return McpErrorHelper.Serialize(ex);
        }
    }

    [McpServerTool, Description("Rename an existing tag.")]
    public async Task<string> UpdateTag(
        UpdateTag updateTag,
        [Description("The ID of the tag to rename")] string tagId,
        [Description("The new name for the tag")] string name)
    {
        if (!Guid.TryParse(tagId, out var parsedTagId))
            return JsonSerializer.Serialize(new { success = false, error = "Invalid tag ID format" });

        try
        {
            var command = new UpdateTag.Command(userContext.UserId, parsedTagId, name);
            await updateTag.ExecuteAsync(command);
            return JsonSerializer.Serialize(new { success = true });
        }
        catch (Exception ex)
        {
            return McpErrorHelper.Serialize(ex);
        }
    }

    [McpServerTool, Description("Delete a tag. Removes it from all tasks, notes, and meetings.")]
    public async Task<string> DeleteTag(
        DeleteTag deleteTag,
        [Description("The ID of the tag to delete")] string tagId)
    {
        if (!Guid.TryParse(tagId, out var parsedTagId))
            return JsonSerializer.Serialize(new { success = false, error = "Invalid tag ID format" });

        try
        {
            var command = new DeleteTag.Command(userContext.UserId, parsedTagId);
            await deleteTag.ExecuteAsync(command);
            return JsonSerializer.Serialize(new { success = true });
        }
        catch (Exception ex)
        {
            return McpErrorHelper.Serialize(ex);
        }
    }

    [McpServerTool, Description("Get all items (tasks, notes, meetings) that have a specific tag.")]
    public async Task<string> GetItemsByTag(
        GetItemsByTag getItemsByTag,
        [Description("The ID of the tag")] string tagId)
    {
        if (!Guid.TryParse(tagId, out var parsedTagId))
            return JsonSerializer.Serialize(new { success = false, error = "Invalid tag ID format" });

        try
        {
            var query = new GetItemsByTag.Query(userContext.UserId, parsedTagId);
            var result = await getItemsByTag.ExecuteAsync(query);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return McpErrorHelper.Serialize(ex);
        }
    }
}
