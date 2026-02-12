using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PraxisNote.Application.Features.Goals;
using PraxisNote.Application.Features.Insights;
using PraxisNote.Domain.Aggregates.BehavioralGoals;
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
        group.MapGet("/communication-profile", (Delegate)HandleGetCommunicationProfile);
        group.MapGet("/johari-window", (Delegate)HandleGetJohariWindow);

        group.MapGet("/nudges", (Delegate)HandleGetNudges);
        group.MapPost("/nudges/{id:guid}/dismiss", (Delegate)HandleDismissNudge);
        group.MapPost("/nudges/{id:guid}/accept", (Delegate)HandleAcceptNudge);

        group.MapGet("/goals", (Delegate)HandleGetGoals);
        group.MapGet("/goals/progress", (Delegate)HandleGetGoalProgress);
        group.MapPost("/goals", (Delegate)HandleCreateGoal);
        group.MapPut("/goals/{id:guid}", (Delegate)HandleUpdateGoal);
        group.MapDelete("/goals/{id:guid}", (Delegate)HandleDeleteGoal);
    }

    private static async Task<IResult> HandleGetBehavioralTrends(
        HttpContext context,
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

        var profileId = context.GetProfileId();
        var effectiveParticipant = participant ?? user.GetUserName();
        var query = new GetBehavioralTrends.Query(userId.Value, profileId, effectiveRange, effectiveParticipant);
        var result = await getBehavioralTrends.ExecuteAsync(query, cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetInsightsSummary(
        HttpContext context,
        ClaimsPrincipal user,
        [FromServices] GetInsightsSummary getInsightsSummary,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var profileId = context.GetProfileId();
        var query = new GetInsightsSummary.Query(userId.Value, profileId);
        var result = await getInsightsSummary.ExecuteAsync(query, cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetCommunicationProfile(
        HttpContext context,
        ClaimsPrincipal user,
        [FromQuery] string? range,
        [FromServices] GetCommunicationProfile getCommunicationProfile,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var effectiveRange = range ?? "90d";
        if (!GetCommunicationProfile.ValidRanges.Contains(effectiveRange, StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Invalid range. Use: 7d, 30d, 90d, all");
        }

        var profileId = context.GetProfileId();
        var query = new GetCommunicationProfile.Query(userId.Value, profileId, effectiveRange);
        var result = await getCommunicationProfile.ExecuteAsync(query, cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetJohariWindow(
        HttpContext context,
        ClaimsPrincipal user,
        [FromQuery] string? range,
        [FromServices] GetJohariWindow getJohariWindow,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var effectiveRange = range ?? "90d";
        if (!GetJohariWindow.ValidRanges.Contains(effectiveRange, StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Invalid range. Use: 7d, 30d, 90d, all");
        }

        var profileId = context.GetProfileId();
        var query = new GetJohariWindow.Query(userId.Value, profileId, effectiveRange);
        var result = await getJohariWindow.ExecuteAsync(query, cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetNudges(
        HttpContext context,
        ClaimsPrincipal user,
        [FromQuery] string? range,
        [FromServices] GenerateBlindSpotNudges generateNudges,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        var effectiveRange = range ?? "90d";
        if (!GetJohariWindow.ValidRanges.Contains(effectiveRange, StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Invalid range. Use: 7d, 30d, 90d, all");
        }

        var profileId = context.GetProfileId();
        var query = new GenerateBlindSpotNudges.Query(userId.Value, profileId, effectiveRange);
        var nudges = await generateNudges.ExecuteAsync(query, cancellationToken);

        return Results.Ok(nudges);
    }

    private static async Task<IResult> HandleDismissNudge(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] DismissNudge dismissNudge,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        try
        {
            var command = new DismissNudge.Command(userId.Value, id);
            await dismissNudge.ExecuteAsync(command, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == DismissNudge.NotFoundError)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> HandleAcceptNudge(
        Guid id,
        HttpContext context,
        ClaimsPrincipal user,
        [FromServices] AcceptNudgeAsGoal acceptNudge,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        try
        {
            var profileId = context.GetProfileId();
            var command = new AcceptNudgeAsGoal.Command(userId.Value, profileId, id);
            var goalId = await acceptNudge.ExecuteAsync(command, cancellationToken);
            return Results.Ok(new { goalId });
        }
        catch (InvalidOperationException ex) when (ex.Message == AcceptNudgeAsGoal.NotFoundError)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> HandleGetGoals(
        HttpContext context,
        ClaimsPrincipal user,
        [FromServices] GetUserGoals getUserGoals,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        var profileId = context.GetProfileId();
        var query = new GetUserGoals.Query(userId.Value, profileId);
        var goals = await getUserGoals.ExecuteAsync(query, cancellationToken);

        return Results.Ok(goals);
    }

    private static async Task<IResult> HandleGetGoalProgress(
        HttpContext context,
        ClaimsPrincipal user,
        [FromServices] EvaluateGoalProgress evaluateGoalProgress,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        var profileId = context.GetProfileId();
        var query = new EvaluateGoalProgress.Query(userId.Value, profileId);
        var progress = await evaluateGoalProgress.ExecuteAsync(query, cancellationToken);

        return Results.Ok(progress);
    }

    private static async Task<IResult> HandleCreateGoal(
        HttpContext context,
        ClaimsPrincipal user,
        CreateGoalRequest request,
        [FromServices] CreateBehavioralGoal createGoal,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(new { error = "Title is required" });

        if (!Enum.TryParse<MetricType>(request.MetricType, true, out var metricType))
            return Results.BadRequest(new { error = "Invalid metric type" });

        if (!Enum.TryParse<GoalOperator>(request.Operator, true, out var goalOperator))
            return Results.BadRequest(new { error = "Invalid operator" });

        if (goalOperator == GoalOperator.Between &&
            (request.TargetValueUpper is null || request.TargetValueUpper <= request.TargetValue))
            return Results.BadRequest(new { error = "Between operator requires an upper bound greater than the target value" });

        try
        {
            var profileId = context.GetProfileId();
            var command = new CreateBehavioralGoal.Command(
                userId.Value, profileId, metricType, goalOperator,
                request.TargetValue, request.TargetValueUpper, request.Title);
            var result = await createGoal.ExecuteAsync(command, cancellationToken);

            return Results.Created($"/api/insights/goals/{result.GoalId}", new { id = result.GoalId });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> HandleUpdateGoal(
        Guid id,
        ClaimsPrincipal user,
        UpdateGoalRequest request,
        [FromServices] UpdateBehavioralGoal updateGoal,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(new { error = "Title is required" });

        if (!Enum.TryParse<MetricType>(request.MetricType, true, out var metricType))
            return Results.BadRequest(new { error = "Invalid metric type" });

        if (!Enum.TryParse<GoalOperator>(request.Operator, true, out var goalOperator))
            return Results.BadRequest(new { error = "Invalid operator" });

        if (goalOperator == GoalOperator.Between &&
            (request.TargetValueUpper is null || request.TargetValueUpper <= request.TargetValue))
            return Results.BadRequest(new { error = "Between operator requires an upper bound greater than the target value" });

        try
        {
            var command = new UpdateBehavioralGoal.Command(
                userId.Value, id, metricType, goalOperator,
                request.TargetValue, request.TargetValueUpper, request.Title, request.IsActive);
            await updateGoal.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == UpdateBehavioralGoal.NotFoundError)
        {
            return Results.NotFound();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> HandleDeleteGoal(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] DeleteBehavioralGoal deleteGoal,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        try
        {
            var command = new DeleteBehavioralGoal.Command(userId.Value, id);
            await deleteGoal.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == DeleteBehavioralGoal.NotFoundError)
        {
            return Results.NotFound();
        }
    }
}

public record CreateGoalRequest(
    string MetricType,
    string Operator,
    double TargetValue,
    double? TargetValueUpper,
    string Title);

public record UpdateGoalRequest(
    string MetricType,
    string Operator,
    double TargetValue,
    double? TargetValueUpper,
    string Title,
    bool IsActive);
