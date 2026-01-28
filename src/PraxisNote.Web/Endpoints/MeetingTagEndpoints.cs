using System.Security.Claims;
using PraxisNote.Web.Extensions;
using PraxisNote.Application.Features.Meetings;

namespace PraxisNote.Web.Endpoints;

public static class MeetingTagEndpoints
{
    public static void MapMeetingTagEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/meetings/{meetingId:guid}/tags")
            .RequireAuthorization();

        group.MapPost("/{tagId:guid}", (Delegate)HandleAddTag);
        group.MapDelete("/{tagId:guid}", (Delegate)HandleRemoveTag);
    }

    private static async Task<IResult> HandleAddTag(
        Guid meetingId,
        Guid tagId,
        ClaimsPrincipal user,
        AddTagToMeeting addTagToMeeting,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var command = new AddTagToMeeting.Command(userId.Value, meetingId, tagId);
            await addTagToMeeting.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == AddTagToMeeting.MeetingNotFoundError)
        {
            return Results.NotFound(new { error = "Meeting not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message == AddTagToMeeting.TagNotFoundError)
        {
            return Results.NotFound(new { error = "Tag not found" });
        }
    }

    private static async Task<IResult> HandleRemoveTag(
        Guid meetingId,
        Guid tagId,
        ClaimsPrincipal user,
        RemoveTagFromMeeting removeTagFromMeeting,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var command = new RemoveTagFromMeeting.Command(userId.Value, meetingId, tagId);
            await removeTagFromMeeting.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == RemoveTagFromMeeting.MeetingNotFoundError)
        {
            return Results.NotFound(new { error = "Meeting not found" });
        }
    }
}
