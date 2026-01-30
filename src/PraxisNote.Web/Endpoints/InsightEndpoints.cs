using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PraxisNote.Application.Features.Insights;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class InsightEndpoints
{
    private static readonly string[] ValidRanges = ["7d", "30d", "90d", "all"];

    public static void MapInsightEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/insights")
            .RequireAuthorization();

        group.MapGet("/behavioral-trends", (Delegate)HandleGetBehavioralTrends);
    }

    private static async Task<IResult> HandleGetBehavioralTrends(
        ClaimsPrincipal user,
        [FromQuery] string range,
        [FromQuery] string? participant,
        [FromServices] GetBehavioralTrends getBehavioralTrends,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (!ValidRanges.Contains(range))
        {
            return Results.BadRequest("Invalid range. Use: 7d, 30d, 90d, all");
        }

        var query = new GetBehavioralTrends.Query(userId.Value, range, participant);
        var result = await getBehavioralTrends.ExecuteAsync(query, cancellationToken);

        return Results.Ok(result);
    }
}
