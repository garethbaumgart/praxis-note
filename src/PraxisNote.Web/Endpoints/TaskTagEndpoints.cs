using System.Security.Claims;
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
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new AddTagToTask.Command(taskId, tagId, userId.Value);
        var result = await addTagToTask.ExecuteAsync(command, cancellationToken);

        if (!result.Success)
        {
            return result.Error switch
            {
                AddTagToTask.ErrorCode.TaskNotFound or AddTagToTask.ErrorCode.TagNotFound
                    => Results.NotFound(new { error = result.Message }),
                _ => Results.BadRequest(new { error = result.Message }),
            };
        }

        return Results.NoContent();
    }

    private static async Task<IResult> HandleRemoveTag(
        Guid taskId,
        Guid tagId,
        ClaimsPrincipal user,
        RemoveTagFromTask removeTagFromTask,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new RemoveTagFromTask.Command(taskId, tagId, userId.Value);
        var success = await removeTagFromTask.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}
