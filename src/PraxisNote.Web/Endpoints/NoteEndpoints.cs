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
    }

    private static async Task<IResult> HandleGetNotes(
        ClaimsPrincipal user,
        GetUserNotes getUserNotes,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetUserNotes.Query(userId.Value);
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

        var command = new CreateNote.Command(userId.Value, request.Content);
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
}

public record CreateNoteRequest(string? Content);
public record UpdateNoteRequest(string? Content);
