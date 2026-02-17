using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Web.Mcp;

namespace PraxisNote.Web.Mcp.Tools;

[McpServerToolType]
public sealed class MeetingTools(McpUserContext userContext)
{
    [McpServerTool, Description("List all meetings for the current user. Returns meetings with their analysis, action items, and tags.")]
    public async Task<string> ListMeetings(GetUserMeetings getUserMeetings)
    {
        try
        {
            var query = new GetUserMeetings.Query(userContext.UserId, userContext.ProfileId);
            var meetings = await getUserMeetings.ExecuteAsync(query);
            return JsonSerializer.Serialize(meetings);
        }
        catch (Exception ex)
        {
            return McpErrorHelper.Serialize(ex);
        }
    }

    [McpServerTool, Description("Get a single meeting by ID with full details including transcript and analysis.")]
    public async Task<string> GetMeeting(
        GetMeetingById getMeetingById,
        [Description("The ID of the meeting")] string meetingId)
    {
        if (!Guid.TryParse(meetingId, out var parsedMeetingId))
            return JsonSerializer.Serialize(new { error = "Invalid meeting ID format" });
        try
        {
            var query = new GetMeetingById.Query(parsedMeetingId, userContext.UserId);
            var meeting = await getMeetingById.ExecuteAsync(query);
            return meeting is null
                ? JsonSerializer.Serialize(new { error = "Meeting not found" })
                : JsonSerializer.Serialize(meeting);
        }
        catch (Exception ex)
        {
            return McpErrorHelper.Serialize(ex);
        }
    }

    [McpServerTool, Description("Create a new meeting.")]
    public async Task<string> CreateMeeting(
        CreateMeeting createMeeting,
        [Description("Optional meeting title")] string? title = null,
        [Description("Optional meeting date in ISO 8601 format")] string? meetingDate = null,
        [Description("Optional comma-separated list of attendees")] string? attendees = null)
    {
        DateTimeOffset? parsedDate = null;
        if (meetingDate is not null)
        {
            if (!DateTimeOffset.TryParse(meetingDate, out var d))
                return JsonSerializer.Serialize(new { error = "Invalid date format. Use ISO 8601." });
            parsedDate = d;
        }
        try
        {
            var command = new CreateMeeting.Command(userContext.UserId, userContext.ProfileId, title, parsedDate, attendees);
            var result = await createMeeting.ExecuteAsync(command);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return McpErrorHelper.Serialize(ex);
        }
    }

    [McpServerTool, Description("Update meeting details (title, date, attendees).")]
    public async Task<string> UpdateMeeting(
        UpdateMeeting updateMeeting,
        [Description("The ID of the meeting to update")] string meetingId,
        [Description("New title (null to keep existing)")] string? title = null,
        [Description("New date in ISO 8601 format (null to keep existing)")] string? meetingDate = null,
        [Description("New comma-separated attendees (null to keep existing)")] string? attendees = null)
    {
        if (!Guid.TryParse(meetingId, out var parsedMeetingId))
            return JsonSerializer.Serialize(new { success = false, error = "Invalid meeting ID format" });
        DateTimeOffset? parsedDate = null;
        if (meetingDate is not null)
        {
            if (!DateTimeOffset.TryParse(meetingDate, out var d))
                return JsonSerializer.Serialize(new { success = false, error = "Invalid date format. Use ISO 8601." });
            parsedDate = d;
        }
        try
        {
            var command = new UpdateMeeting.Command(parsedMeetingId, userContext.UserId, title, parsedDate, attendees);
            var success = await updateMeeting.ExecuteAsync(command);
            return JsonSerializer.Serialize(new { success });
        }
        catch (Exception ex)
        {
            return McpErrorHelper.Serialize(ex);
        }
    }

    [McpServerTool, Description("Delete a meeting permanently.")]
    public async Task<string> DeleteMeeting(
        DeleteMeeting deleteMeeting,
        [Description("The ID of the meeting to delete")] string meetingId)
    {
        if (!Guid.TryParse(meetingId, out var parsedMeetingId))
            return JsonSerializer.Serialize(new { success = false, error = "Invalid meeting ID format" });
        try
        {
            var command = new DeleteMeeting.Command(parsedMeetingId, userContext.UserId);
            var success = await deleteMeeting.ExecuteAsync(command);
            return JsonSerializer.Serialize(new { success });
        }
        catch (Exception ex)
        {
            return McpErrorHelper.Serialize(ex);
        }
    }

    [McpServerTool, Description("Add or remove a tag from a meeting.")]
    public async Task<string> ManageMeetingTag(
        AddTagToMeeting addTag,
        RemoveTagFromMeeting removeTag,
        [Description("The ID of the meeting")] string meetingId,
        [Description("The ID of the tag")] string tagId,
        [Description("Action: 'add' or 'remove'")] string action)
    {
        if (!Guid.TryParse(meetingId, out var parsedMeetingId))
            return JsonSerializer.Serialize(new { success = false, error = "Invalid meeting ID format" });
        if (!Guid.TryParse(tagId, out var parsedTagId))
            return JsonSerializer.Serialize(new { success = false, error = "Invalid tag ID format" });

        try
        {
            if (action.Equals("add", StringComparison.OrdinalIgnoreCase))
            {
                await addTag.ExecuteAsync(new AddTagToMeeting.Command(userContext.UserId, parsedMeetingId, parsedTagId));
                return JsonSerializer.Serialize(new { success = true, action = "added" });
            }

            if (action.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                await removeTag.ExecuteAsync(new RemoveTagFromMeeting.Command(userContext.UserId, parsedMeetingId, parsedTagId));
                return JsonSerializer.Serialize(new { success = true, action = "removed" });
            }

            return JsonSerializer.Serialize(new { success = false, error = $"Invalid action '{action}'. Use 'add' or 'remove'." });
        }
        catch (Exception ex)
        {
            return McpErrorHelper.Serialize(ex);
        }
    }
}
