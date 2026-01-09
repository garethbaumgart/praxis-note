using System.Security.Claims;
using PraxisNote.Application.Features.Tasks;

namespace PraxisNote.Web.Endpoints;

public static class CommentEndpoints
{
    public static void MapCommentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tasks/{taskId:guid}/comments")
            .RequireAuthorization();

        group.MapPost("/", (Delegate)HandleAddComment);
        group.MapPut("/{commentId:guid}", (Delegate)HandleUpdateComment);
        group.MapDelete("/{commentId:guid}", (Delegate)HandleDeleteComment);
    }

    private static async Task<IResult> HandleAddComment(
        Guid taskId,
        ClaimsPrincipal user,
        AddCommentRequest request,
        AddComment addComment,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest(new { error = "Content is required" });
        }

        var command = new AddComment.Command(taskId, userId.Value, request.Content);
        var result = await addComment.ExecuteAsync(command, cancellationToken);

        return result is not null
            ? Results.Created($"/api/tasks/{taskId}/comments/{result.CommentId}", new { id = result.CommentId })
            : Results.NotFound();
    }

    private static async Task<IResult> HandleUpdateComment(
        Guid taskId,
        Guid commentId,
        ClaimsPrincipal user,
        UpdateCommentRequest request,
        UpdateComment updateComment,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest(new { error = "Content is required" });
        }

        var command = new UpdateComment.Command(taskId, commentId, userId.Value, request.Content);
        var success = await updateComment.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleDeleteComment(
        Guid taskId,
        Guid commentId,
        ClaimsPrincipal user,
        DeleteComment deleteComment,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new DeleteComment.Command(taskId, commentId, userId.Value);
        var success = await deleteComment.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}

public record AddCommentRequest(string Content);
public record UpdateCommentRequest(string Content);
