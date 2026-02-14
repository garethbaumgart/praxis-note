using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PraxisNote.Application.Features.ActionItems;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class ActionItemEndpoints
{
    public static void MapActionItemEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/action-items")
            .RequireAuthorization();

        group.MapGet("/outstanding", (Delegate)HandleGetOutstandingActionItems);
    }

    private static async Task<IResult> HandleGetOutstandingActionItems(
        HttpContext context,
        ClaimsPrincipal user,
        [FromServices] GetOutstandingActionItems getOutstandingActionItems,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        var profileId = context.GetProfileId();
        var query = new GetOutstandingActionItems.Query(userId.Value, profileId);
        var result = await getOutstandingActionItems.ExecuteAsync(query, cancellationToken);

        return Results.Ok(result);
    }
}
