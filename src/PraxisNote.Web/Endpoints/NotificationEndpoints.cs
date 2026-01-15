using System.Security.Claims;
using PraxisNote.Application.Features.Notifications;
using PraxisNote.Web.Services;

namespace PraxisNote.Web.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/notifications")
            .RequireAuthorization();

        group.MapGet("/", (Delegate)HandleGetNotifications);
        group.MapGet("/count", (Delegate)HandleGetUnseenCount);
        group.MapPost("/seen", (Delegate)HandleMarkSeen);
        group.MapGet("/stream", (Delegate)HandleSseStream);
    }

    private static async Task<IResult> HandleGetNotifications(
        ClaimsPrincipal user,
        GetNotifications getNotifications,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var notifications = await getNotifications.ExecuteAsync(
            new GetNotifications.Query(userId.Value),
            cancellationToken);

        return Results.Ok(notifications);
    }

    private static async Task<IResult> HandleGetUnseenCount(
        ClaimsPrincipal user,
        GetUnseenNotificationCount getCount,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var count = await getCount.ExecuteAsync(
            new GetUnseenNotificationCount.Query(userId.Value),
            cancellationToken);

        return Results.Ok(new { count });
    }

    private static async Task<IResult> HandleMarkSeen(
        ClaimsPrincipal user,
        MarkSeenRequest request,
        MarkNotificationsSeen markSeen,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        await markSeen.ExecuteAsync(
            new MarkNotificationsSeen.Command(userId.Value, request.LastSeenNotificationId),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task HandleSseStream(
        HttpContext context,
        ClaimsPrincipal user,
        NotificationSseManager sseManager,
        GetUnseenNotificationCount getCount,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            context.Response.StatusCode = 401;
            return;
        }

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        sseManager.AddConnection(userId.Value, context.Response);

        try
        {
            // Send initial count
            var count = await getCount.ExecuteAsync(
                new GetUnseenNotificationCount.Query(userId.Value),
                cancellationToken);

            var initialData = $"event: count\ndata: {{\"count\":{count}}}\n\n";
            await context.Response.WriteAsync(initialData, cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);

            // Keep connection open until client disconnects
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
        finally
        {
            sseManager.RemoveConnection(userId.Value, context.Response);
        }
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}

public record MarkSeenRequest(int LastSeenNotificationId);
