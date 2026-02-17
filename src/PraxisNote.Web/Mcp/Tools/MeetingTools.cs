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
        var query = new GetUserMeetings.Query(userContext.UserId, userContext.ProfileId);
        var meetings = await getUserMeetings.ExecuteAsync(query);
        return JsonSerializer.Serialize(meetings);
    }

    [McpServerTool, Description("Get a single meeting by ID with full details including transcript and analysis.")]
    public async Task<string> GetMeeting(
        GetMeetingById getMeetingById,
        [Description("The ID of the meeting")] string meetingId)
    {
        var query = new GetMeetingById.Query(Guid.Parse(meetingId), userContext.UserId);
        var meeting = await getMeetingById.ExecuteAsync(query);
        return meeting is null
            ? JsonSerializer.Serialize(new { error = "Meeting not found" })
            : JsonSerializer.Serialize(meeting);
    }

    [McpServerTool, Description("Create a new meeting.")]
    public async Task<string> CreateMeeting(
        CreateMeeting createMeeting,
        [Description("Optional meeting title")] string? title = null,
        [Description("Optional meeting date in ISO 8601 format")] string? meetingDate = null,
        [Description("Optional comma-separated list of attendees")] string? attendees = null)
    {
        DateTimeOffset? parsedDate = meetingDate is not null ? DateTimeOffset.Parse(meetingDate) : null;
        var command = new CreateMeeting.Command(userContext.UserId, userContext.ProfileId, title, parsedDate, attendees);
        var result = await createMeeting.ExecuteAsync(command);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool, Description("Update meeting details (title, date, attendees).")]
    public async Task<string> UpdateMeeting(
        UpdateMeeting updateMeeting,
        [Description("The ID of the meeting to update")] string meetingId,
        [Description("New title (null to keep existing)")] string? title = null,
        [Description("New date in ISO 8601 format (null to keep existing)")] string? meetingDate = null,
        [Description("New comma-separated attendees (null to keep existing)")] string? attendees = null)
    {
        DateTimeOffset? parsedDate = meetingDate is not null ? DateTimeOffset.Parse(meetingDate) : null;
        var command = new UpdateMeeting.Command(Guid.Parse(meetingId), userContext.UserId, title, parsedDate, attendees);
        var success = await updateMeeting.ExecuteAsync(command);
        return JsonSerializer.Serialize(new { success });
    }

    [McpServerTool, Description("Delete a meeting permanently.")]
    public async Task<string> DeleteMeeting(
        DeleteMeeting deleteMeeting,
        [Description("The ID of the meeting to delete")] string meetingId)
    {
        var command = new DeleteMeeting.Command(Guid.Parse(meetingId), userContext.UserId);
        var success = await deleteMeeting.ExecuteAsync(command);
        return JsonSerializer.Serialize(new { success });
    }

    [McpServerTool, Description("Add or remove a tag from a meeting.")]
    public async Task<string> ManageMeetingTag(
        AddTagToMeeting addTag,
        RemoveTagFromMeeting removeTag,
        [Description("The ID of the meeting")] string meetingId,
        [Description("The ID of the tag")] string tagId,
        [Description("Action: 'add' or 'remove'")] string action)
    {
        var parsedMeetingId = Guid.Parse(meetingId);
        var parsedTagId = Guid.Parse(tagId);

        if (action.Equals("add", StringComparison.OrdinalIgnoreCase))
        {
            await addTag.ExecuteAsync(new AddTagToMeeting.Command(userContext.UserId, parsedMeetingId, parsedTagId));
            return JsonSerializer.Serialize(new { success = true, action = "added" });
        }

        await removeTag.ExecuteAsync(new RemoveTagFromMeeting.Command(userContext.UserId, parsedMeetingId, parsedTagId));
        return JsonSerializer.Serialize(new { success = true, action = "removed" });
    }
}
