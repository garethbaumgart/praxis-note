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
        group.MapDelete("/{id:guid}/transcript", (Delegate)HandleClearTranscript);
        group.MapPost("/{id:guid}/analyze", (Delegate)HandleAnalyzeMeeting);
        group.MapPatch("/{id:guid}/action-items/{actionItemId:guid}/toggle", (Delegate)HandleToggleActionItem);
        group.MapPost("/{id:guid}/action-items/{actionItemId:guid}/promote", (Delegate)HandlePromoteActionItem);
        group.MapGet("/{id:guid}/action-item-status", (Delegate)HandleGetActionItemStatus);
        group.MapGet("/{id:guid}/reflection/prompts", (Delegate)HandleGetReflectionPrompts);
        group.MapGet("/{id:guid}/reflection", (Delegate)HandleGetReflection);
        group.MapPost("/{id:guid}/reflection", (Delegate)HandleSubmitReflection);
        group.MapPost("/extract-from-screenshot", (Delegate)HandleExtractFromScreenshot);
        group.MapPatch("/{id:guid}/exclude-from-insights", (Delegate)HandleUpdateExcludeFromInsights);
    }

    private static async Task<IResult> HandleGetMeetings(
        HttpContext context,
        ClaimsPrincipal user,
        [FromServices] GetUserMeetings getUserMeetings,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var profileId = context.GetProfileId();
        var query = new GetUserMeetings.Query(userId.Value, profileId);
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
        HttpContext context,
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

        var profileId = context.GetProfileId();
        var command = new CreateMeeting.Command(
            userId.Value,
            profileId,
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

    private static async Task<IResult> HandleClearTranscript(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] ClearTranscript clearTranscript,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new ClearTranscript.Command(id, userId.Value);
        var success = await clearTranscript.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleAnalyzeMeeting(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] AnalyzeMeeting analyzeMeeting,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new AnalyzeMeeting.Command(id, userId.Value);
        var success = await analyzeMeeting.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleToggleActionItem(
        Guid id,
        Guid actionItemId,
        ClaimsPrincipal user,
        [FromServices] ToggleActionItem toggleActionItem,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var command = new ToggleActionItem.Command(userId.Value, id, actionItemId);
            await toggleActionItem.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == ToggleActionItem.MeetingNotFoundError)
        {
            return Results.NotFound(new { error = "Meeting not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message == ToggleActionItem.ActionItemNotFoundError)
        {
            return Results.NotFound(new { error = "Action item not found" });
        }
    }

    private static async Task<IResult> HandlePromoteActionItem(
        Guid id,
        Guid actionItemId,
        ClaimsPrincipal user,
        [FromServices] PromoteActionItemToTask promoteActionItemToTask,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new PromoteActionItemToTask.Command(id, userId.Value, actionItemId);
        var result = await promoteActionItemToTask.ExecuteAsync(command, cancellationToken);

        if (result is null)
        {
            return Results.NotFound();
        }

        return Results.Created($"/api/tasks/{result.TaskId}", result);
    }

    private static async Task<IResult> HandleGetActionItemStatus(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] GetActionItemStatus getActionItemStatus,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetActionItemStatus.Query(id, userId.Value);
        var result = await getActionItemStatus.ExecuteAsync(query, cancellationToken);

        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> HandleGetReflectionPrompts(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] GenerateReflectionPrompts generateReflectionPrompts,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GenerateReflectionPrompts.Query(id, userId.Value);
        var result = await generateReflectionPrompts.ExecuteAsync(query, cancellationToken);

        return result is not null ? Results.Ok(result.Prompts) : Results.NotFound();
    }

    private static async Task<IResult> HandleGetReflection(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] GetMeetingReflection getMeetingReflection,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetMeetingReflection.Query(id, userId.Value);
        var result = await getMeetingReflection.ExecuteAsync(query, cancellationToken);

        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> HandleSubmitReflection(
        Guid id,
        ClaimsPrincipal user,
        SubmitReflectionRequest request,
        [FromServices] SubmitReflection submitReflection,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var promptResponses = request.PromptResponses?
            .Select(p => new PromptResponseDto(p.PromptId, p.PromptText, p.Response))
            .ToList() ?? [];

        var command = new SubmitReflection.Command(
            id,
            userId.Value,
            request.SelfAssessedTalkTime,
            request.SelfAssessedEngagement,
            request.SelfAssessedTone,
            request.InterruptionAwareness,
            request.FreeformReflection,
            promptResponses);

        var success = await submitReflection.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleExtractFromScreenshot(
        ClaimsPrincipal user,
        ExtractFromScreenshotRequest request,
        [FromServices] ExtractMeetingsFromScreenshot extractMeetings,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Base64Image))
        {
            return Results.BadRequest("Image data is required.");
        }

        var mediaType = request.MediaType ?? "image/png";
        string[] allowedMediaTypes = ["image/png", "image/jpeg", "image/webp"];
        if (!allowedMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Unsupported media type. Supported types: image/png, image/jpeg, image/webp.");
        }

        // Limit base64 payload to ~10MB (base64 overhead means ~7.5MB actual image)
        if (request.Base64Image.Length > 10_000_000)
        {
            return Results.BadRequest("Image is too large. Maximum size is 10MB.");
        }

        var timeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? null : request.TimeZone.Trim();
        if (timeZone is not null && (timeZone.Length > 64 || timeZone.Any(char.IsControl)))
        {
            return Results.BadRequest("Invalid time zone.");
        }

        var command = new ExtractMeetingsFromScreenshot.Command(
            userId.Value, request.Base64Image, mediaType, timeZone);
        var result = await extractMeetings.ExecuteAsync(command, cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdateExcludeFromInsights(
        Guid id,
        ClaimsPrincipal user,
        ExcludeFromInsightsRequest request,
        [FromServices] UpdateMeetingExcludeFromInsights updateExclude,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateMeetingExcludeFromInsights.Command(id, userId.Value, request.Exclude);
        var success = await updateExclude.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }
}

public record CreateMeetingRequest(string? Title, DateTimeOffset? MeetingDate, string? Attendees);
public record UpdateMeetingRequest(string? Title, DateTimeOffset? MeetingDate, string? Attendees);
public record SubmitTranscriptRequest(string Transcript);

public record SubmitReflectionRequest(
    int? SelfAssessedTalkTime,
    string? SelfAssessedEngagement,
    string? SelfAssessedTone,
    string? InterruptionAwareness,
    string? FreeformReflection,
    List<PromptResponseRequest>? PromptResponses);

public record PromptResponseRequest(string PromptId, string PromptText, string Response);

public record ExcludeFromInsightsRequest(bool Exclude);
public record ExtractFromScreenshotRequest(string Base64Image, string? MediaType, string? TimeZone);
