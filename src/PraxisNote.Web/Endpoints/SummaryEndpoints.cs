using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PraxisNote.Application.Features.Summary;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class SummaryEndpoints
{
    public static void MapSummaryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/summary")
            .RequireAuthorization();

        group.MapGet("/", (Delegate)HandleGetDailySummary);
    }

    private static async Task<IResult> HandleGetDailySummary(
        ClaimsPrincipal user,
        [FromQuery] string? date,
        [FromServices] GetDailySummary getDailySummary,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        DateOnly targetDate;
        if (string.IsNullOrWhiteSpace(date))
        {
            targetDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }
        else if (!DateOnly.TryParse(date, out targetDate))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });
        }

        var query = new GetDailySummary.Query(userId.Value, targetDate);
        var result = await getDailySummary.ExecuteAsync(query, cancellationToken);

        return Results.Ok(result);
    }
}
