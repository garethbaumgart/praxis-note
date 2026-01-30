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
        group.MapGet("/summary", (Delegate)HandleGetInsightsSummary);
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

    private static async Task<IResult> HandleGetInsightsSummary(
        ClaimsPrincipal user,
        [FromServices] GetInsightsSummary getInsightsSummary,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetInsightsSummary.Query(userId.Value);
        var result = await getInsightsSummary.ExecuteAsync(query, cancellationToken);

        return Results.Ok(result);
    }
}
