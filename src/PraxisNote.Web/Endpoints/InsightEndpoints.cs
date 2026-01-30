using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PraxisNote.Application.Features.Insights;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class InsightEndpoints
{
    public static void MapInsightEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/insights")
            .RequireAuthorization();

        group.MapGet("/behavioral-trends", (Delegate)HandleGetBehavioralTrends);
    }

    private static async Task<IResult> HandleGetBehavioralTrends(
        ClaimsPrincipal user,
        [FromQuery] string? range,
        [FromQuery] string? participant,
        [FromServices] GetBehavioralTrends getBehavioralTrends,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var effectiveRange = range ?? "30d";
        if (!GetBehavioralTrends.ValidRanges.Contains(effectiveRange, StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Invalid range. Use: 7d, 30d, 90d, all");
        }

        var query = new GetBehavioralTrends.Query(userId.Value, effectiveRange, participant);
        var result = await getBehavioralTrends.ExecuteAsync(query, cancellationToken);

        return Results.Ok(result);
    }
}
