using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PraxisNote.Application.Features.Tasks;

namespace PraxisNote.Web.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tasks")
            .RequireAuthorization();

        group.MapGet("/", (Delegate)HandleGetTasks);
        group.MapGet("/archived/count", (Delegate)HandleGetArchivedCount);
        group.MapPost("/", (Delegate)HandleCreateTask);
        group.MapPut("/{id:guid}", (Delegate)HandleUpdateTask);
        group.MapPut("/{id:guid}/status", (Delegate)HandleChangeStatus);
        group.MapPatch("/{id:guid}/priority", (Delegate)HandleTogglePriority);
        group.MapDelete("/{id:guid}", (Delegate)HandleDeleteTask);
        group.MapPut("/reorder", (Delegate)HandleReorderTasks);
    }

    private static async Task<IResult> HandleGetTasks(
        ClaimsPrincipal user,
        GetUserTasks getUserTasks,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetUserTasks.Query(userId.Value, includeArchived);
        var tasks = await getUserTasks.ExecuteAsync(query, cancellationToken);

        return Results.Ok(tasks);
    }

    private static async Task<IResult> HandleGetArchivedCount(
        ClaimsPrincipal user,
        GetArchivedCount getArchivedCount,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetArchivedCount.Query(userId.Value);
        var count = await getArchivedCount.ExecuteAsync(query, cancellationToken);

        return Results.Ok(new { count });
    }

    private static async Task<IResult> HandleCreateTask(
        ClaimsPrincipal user,
        CreateTaskRequest request,
        CreateTask createTask,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest(new { error = "Title is required" });
        }

        var command = new CreateTask.Command(userId.Value, request.Title);
        var result = await createTask.ExecuteAsync(command, cancellationToken);

        return Results.Created($"/api/tasks/{result.TaskId}", new { id = result.TaskId });
    }

    private static async Task<IResult> HandleUpdateTask(
        Guid id,
        ClaimsPrincipal user,
        UpdateTaskRequest request,
        UpdateTask updateTask,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest(new { error = "Title is required" });
        }

        var command = new UpdateTask.Command(id, userId.Value, request.Title);
        var success = await updateTask.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleChangeStatus(
        Guid id,
        ClaimsPrincipal user,
        ChangeStatusRequest request,
        ChangeTaskStatus changeStatus,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return Results.BadRequest(new { error = "Status is required" });
        }

        var command = new ChangeTaskStatus.Command(id, userId.Value, request.Status, request.Position);
        var success = await changeStatus.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleTogglePriority(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] ToggleTaskPriority togglePriority,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new ToggleTaskPriority.Command(id, userId.Value);
        var success = await togglePriority.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleDeleteTask(
        Guid id,
        ClaimsPrincipal user,
        DeleteTask deleteTask,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new DeleteTask.Command(id, userId.Value);
        var success = await deleteTask.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleReorderTasks(
        ClaimsPrincipal user,
        ReorderTasksRequest request,
        ReorderTasks reorderTasks,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Status) || request.TaskIds is null || request.TaskIds.Count == 0)
        {
            return Results.BadRequest(new { error = "Status is required and at least one task ID must be provided" });
        }

        var command = new ReorderTasks.Command(userId.Value, request.Status, request.TaskIds);
        var result = await reorderTasks.ExecuteAsync(command, cancellationToken);

        return result.Success
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}

public record CreateTaskRequest(string Title);
public record UpdateTaskRequest(string Title);
public record ChangeStatusRequest(string Status, int? Position = null);
public record ReorderTasksRequest(string Status, IReadOnlyList<Guid> TaskIds);
