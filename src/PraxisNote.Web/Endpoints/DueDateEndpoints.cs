using System.Security.Claims;
using PraxisNote.Application.Features.Tasks;

namespace PraxisNote.Web.Endpoints;

public static class DueDateEndpoints
{
    public static void MapDueDateEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tasks/{taskId:guid}/due-date")
            .RequireAuthorization();

        group.MapPut("/", (Delegate)HandleSetDueDate);
        group.MapDelete("/", (Delegate)HandleClearDueDate);
    }

    private static async Task<IResult> HandleSetDueDate(
        Guid taskId,
        ClaimsPrincipal user,
        SetDueDateRequest request,
        SetDueDate setDueDate,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (!DateOnly.TryParse(request.Date, out var date))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });
        }

        var command = new SetDueDate.Command(taskId, userId.Value, date);
        var success = await setDueDate.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleClearDueDate(
        Guid taskId,
        ClaimsPrincipal user,
        ClearDueDate clearDueDate,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new ClearDueDate.Command(taskId, userId.Value);
        var success = await clearDueDate.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}

public record SetDueDateRequest(string Date);
