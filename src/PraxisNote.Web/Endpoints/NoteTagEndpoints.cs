using System.Security.Claims;
using PraxisNote.Web.Extensions;
using PraxisNote.Application.Features.Notes;

namespace PraxisNote.Web.Endpoints;

public static class NoteTagEndpoints
{
    public static void MapNoteTagEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/notes/{noteId:guid}/tags")
            .RequireAuthorization();

        group.MapPost("/{tagId:guid}", (Delegate)HandleAddTag);
        group.MapDelete("/{tagId:guid}", (Delegate)HandleRemoveTag);
    }

    private static async Task<IResult> HandleAddTag(
        Guid noteId,
        Guid tagId,
        ClaimsPrincipal user,
        AddTagToNote addTagToNote,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var command = new AddTagToNote.Command(userId.Value, noteId, tagId);
            await addTagToNote.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == AddTagToNote.NoteNotFoundError)
        {
            return Results.NotFound(new { error = "Note not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message == AddTagToNote.TagNotFoundError)
        {
            return Results.NotFound(new { error = "Tag not found" });
        }
    }

    private static async Task<IResult> HandleRemoveTag(
        Guid noteId,
        Guid tagId,
        ClaimsPrincipal user,
        RemoveTagFromNote removeTagFromNote,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var command = new RemoveTagFromNote.Command(userId.Value, noteId, tagId);
            await removeTagFromNote.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == RemoveTagFromNote.NoteNotFoundError)
        {
            return Results.NotFound(new { error = "Note not found" });
        }
    }
}
