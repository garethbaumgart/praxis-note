using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class MeetingEndpoints
{
    public static void MapMeetingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/meetings")
            .RequireAuthorization();

        group.MapGet("/", (Delegate)HandleGetMeetings);
        group.MapGet("/{id:guid}", (Delegate)HandleGetMeetingById);
        group.MapPost("/", (Delegate)HandleCreateMeeting);
        group.MapPut("/{id:guid}", (Delegate)HandleUpdateMeeting);
        group.MapDelete("/{id:guid}", (Delegate)HandleDeleteMeeting);
        group.MapPost("/{id:guid}/transcript", (Delegate)HandleSubmitTranscript);
    }

    private static async Task<IResult> HandleGetMeetings(
        ClaimsPrincipal user,
        [FromServices] GetUserMeetings getUserMeetings,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetUserMeetings.Query(userId.Value);
        var meetings = await getUserMeetings.ExecuteAsync(query, cancellationToken);

        return Results.Ok(meetings);
    }

    private static async Task<IResult> HandleGetMeetingById(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] GetMeetingById getMeetingById,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetMeetingById.Query(id, userId.Value);
        var meeting = await getMeetingById.ExecuteAsync(query, cancellationToken);

        return meeting is not null ? Results.Ok(meeting) : Results.NotFound();
    }

    private static async Task<IResult> HandleCreateMeeting(
        ClaimsPrincipal user,
        CreateMeetingRequest request,
        [FromServices] CreateMeeting createMeeting,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new CreateMeeting.Command(
            userId.Value,
            request.Title,
            request.MeetingDate,
            request.Attendees);
        var result = await createMeeting.ExecuteAsync(command, cancellationToken);

        return Results.Created($"/api/meetings/{result.MeetingId}", new { id = result.MeetingId });
    }

    private static async Task<IResult> HandleUpdateMeeting(
        Guid id,
        ClaimsPrincipal user,
        UpdateMeetingRequest request,
        [FromServices] UpdateMeeting updateMeeting,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateMeeting.Command(
            id,
            userId.Value,
            request.Title,
            request.MeetingDate,
            request.Attendees);
        var success = await updateMeeting.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleDeleteMeeting(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] DeleteMeeting deleteMeeting,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new DeleteMeeting.Command(id, userId.Value);
        var success = await deleteMeeting.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleSubmitTranscript(
        Guid id,
        ClaimsPrincipal user,
        SubmitTranscriptRequest request,
        [FromServices] SubmitTranscript submitTranscript,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Transcript))
        {
            return Results.BadRequest("Transcript content is required.");
        }

        var command = new SubmitTranscript.Command(id, userId.Value, request.Transcript);
        var success = await submitTranscript.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }
}

public record CreateMeetingRequest(string? Title, DateTimeOffset? MeetingDate, string? Attendees);
public record UpdateMeetingRequest(string? Title, DateTimeOffset? MeetingDate, string? Attendees);
public record SubmitTranscriptRequest(string Transcript);
