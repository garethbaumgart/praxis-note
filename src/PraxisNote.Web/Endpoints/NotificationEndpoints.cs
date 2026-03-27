using System.Security.Claims;
using PraxisNote.Web.Extensions;
using PraxisNote.Application.Features.Notifications;

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
    }

    private static async Task<IResult> HandleGetNotifications(
        ClaimsPrincipal user,
        GetNotifications getNotifications,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
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
        var userId = user.GetUserId();
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
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        await markSeen.ExecuteAsync(
            new MarkNotificationsSeen.Command(userId.Value, request.LastSeenNotificationId),
            cancellationToken);

        return Results.NoContent();
    }

}

public record MarkSeenRequest(int LastSeenNotificationId);
