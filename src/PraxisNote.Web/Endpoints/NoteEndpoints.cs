using System.Security.Claims;
using PraxisNote.Application.Features.Notes;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class NoteEndpoints
{
    public static void MapNoteEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/notes")
            .RequireAuthorization();

        group.MapGet("/", (Delegate)HandleGetNotes);
        group.MapGet("/{id:guid}", (Delegate)HandleGetNoteById);
        group.MapPost("/", (Delegate)HandleCreateNote);
        group.MapPut("/{id:guid}", (Delegate)HandleUpdateNote);
        group.MapDelete("/{id:guid}", (Delegate)HandleDeleteNote);

        // Checkbox-Task sync endpoints
        group.MapPost("/{noteId:guid}/checkboxes/{checkboxId}/promote", (Delegate)HandlePromoteCheckbox);
        group.MapGet("/{noteId:guid}/checkbox-status", (Delegate)HandleGetCheckboxStatus);
    }

    private static async Task<IResult> HandleGetNotes(
        HttpContext context,
        ClaimsPrincipal user,
        GetUserNotes getUserNotes,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var profileId = context.GetProfileId();
        var query = new GetUserNotes.Query(userId.Value, profileId);
        var notes = await getUserNotes.ExecuteAsync(query, cancellationToken);

        return Results.Ok(notes);
    }

    private static async Task<IResult> HandleGetNoteById(
        Guid id,
        ClaimsPrincipal user,
        GetNoteById getNoteById,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetNoteById.Query(id, userId.Value);
        var note = await getNoteById.ExecuteAsync(query, cancellationToken);

        return note is not null ? Results.Ok(note) : Results.NotFound();
    }

    private static async Task<IResult> HandleCreateNote(
        HttpContext context,
        ClaimsPrincipal user,
        CreateNoteRequest request,
        CreateNote createNote,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var profileId = context.GetProfileId();
        var command = new CreateNote.Command(userId.Value, profileId, request.Content);
        var result = await createNote.ExecuteAsync(command, cancellationToken);

        return Results.Created($"/api/notes/{result.NoteId}", new { id = result.NoteId });
    }

    private static async Task<IResult> HandleUpdateNote(
        Guid id,
        ClaimsPrincipal user,
        UpdateNoteRequest request,
        UpdateNoteContent updateNoteContent,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateNoteContent.Command(id, userId.Value, request.Content ?? string.Empty);
        var success = await updateNoteContent.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleDeleteNote(
        Guid id,
        ClaimsPrincipal user,
        DeleteNote deleteNote,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new DeleteNote.Command(id, userId.Value);
        var success = await deleteNote.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }
    private static async Task<IResult> HandlePromoteCheckbox(
        Guid noteId,
        string checkboxId,
        ClaimsPrincipal user,
        PromoteCheckboxToTask promoteCheckbox,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new PromoteCheckboxToTask.Command(noteId, userId.Value, checkboxId);
        var result = await promoteCheckbox.ExecuteAsync(command, cancellationToken);

        if (result is null)
        {
            return Results.NotFound();
        }

        return Results.Created($"/api/tasks/{result.TaskId}", result);
    }

    private static async Task<IResult> HandleGetCheckboxStatus(
        Guid noteId,
        ClaimsPrincipal user,
        GetCheckboxStatus getCheckboxStatus,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetCheckboxStatus.Query(noteId, userId.Value);
        var result = await getCheckboxStatus.ExecuteAsync(query, cancellationToken);

        if (result is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(result);
    }
}

public record CreateNoteRequest(string? Content);
public record UpdateNoteRequest(string? Content);
