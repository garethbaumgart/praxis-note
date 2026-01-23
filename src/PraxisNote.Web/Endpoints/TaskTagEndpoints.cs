using System.Security.Claims;
using PraxisNote.Web.Extensions;
using PraxisNote.Application.Features.Tasks;

namespace PraxisNote.Web.Endpoints;

public static class TaskTagEndpoints
{
    public static void MapTaskTagEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tasks/{taskId:guid}/tags")
            .RequireAuthorization();

        group.MapPost("/{tagId:guid}", (Delegate)HandleAddTag);
        group.MapDelete("/{tagId:guid}", (Delegate)HandleRemoveTag);
    }

    private static async Task<IResult> HandleAddTag(
        Guid taskId,
        Guid tagId,
        ClaimsPrincipal user,
        AddTagToTask addTagToTask,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var command = new AddTagToTask.Command(userId.Value, taskId, tagId);
            await addTagToTask.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == AddTagToTask.TaskNotFoundError)
        {
            return Results.NotFound(new { error = "Task not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message == AddTagToTask.TagNotFoundError)
        {
            return Results.NotFound(new { error = "Tag not found" });
        }
    }

    private static async Task<IResult> HandleRemoveTag(
        Guid taskId,
        Guid tagId,
        ClaimsPrincipal user,
        RemoveTagFromTask removeTagFromTask,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var command = new RemoveTagFromTask.Command(userId.Value, taskId, tagId);
            await removeTagFromTask.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == RemoveTagFromTask.TaskNotFoundError)
        {
            return Results.NotFound(new { error = "Task not found" });
        }
    }
}
