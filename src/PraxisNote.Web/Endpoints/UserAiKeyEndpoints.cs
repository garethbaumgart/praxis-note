using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using PraxisNote.Application.Features.UserAiKeys;
using PraxisNote.Domain.Aggregates.UserAiKeys;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class UserAiKeyEndpoints
{
    public static void MapUserAiKeyEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/ai-keys")
            .RequireAuthorization();

        group.MapGet("/", (Delegate)HandleGetAiKeys);
        group.MapPut("/{provider}", (Delegate)HandleUpsertAiKey);
        group.MapDelete("/{provider}", (Delegate)HandleDeleteAiKey);
    }

    private static async Task<IResult> HandleGetAiKeys(
        ClaimsPrincipal user,
        GetUserAiKeys getKeys,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var keys = await getKeys.ExecuteAsync(new GetUserAiKeys.Query(userId.Value), cancellationToken);
        return Results.Ok(keys);
    }

    private static async Task<IResult> HandleUpsertAiKey(
        string provider,
        ClaimsPrincipal user,
        UpsertAiKeyRequest request,
        UpsertUserAiKey upsertKey,
        ValidateAiKey validateKey,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (!Enum.TryParse<AiProvider>(provider, ignoreCase: true, out var aiProvider) || !Enum.IsDefined(typeof(AiProvider), aiProvider))
        {
            var validProviders = string.Join(", ", Enum.GetNames(typeof(AiProvider)));
            return Results.BadRequest(new { error = $"Unknown provider. Valid values: {validProviders}" });
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return Results.BadRequest(new { error = "apiKey is required" });
        }

        // Validate the key before persisting
        var validation = await validateKey.ExecuteAsync(
            new ValidateAiKey.Command(aiProvider, request.ApiKey), cancellationToken);

        if (!validation.Validated)
        {
            return Results.UnprocessableEntity(new { error = "ai_key_invalid" });
        }

        // Key is valid — persist it
        var command = new UpsertUserAiKey.Command(userId.Value, aiProvider, request.ApiKey, request.PreferredModel);
        try
        {
            await upsertKey.ExecuteAsync(command, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }

        return Results.Ok(new { validated = validation.Validated, rateLimited = validation.RateLimited });
    }

    private static async Task<IResult> HandleDeleteAiKey(
        string provider,
        ClaimsPrincipal user,
        DeleteUserAiKey deleteKey,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (!Enum.TryParse<AiProvider>(provider, ignoreCase: true, out var aiProvider) || !Enum.IsDefined(typeof(AiProvider), aiProvider))
        {
            var validProviders = string.Join(", ", Enum.GetNames(typeof(AiProvider)));
            return Results.BadRequest(new { error = $"Unknown provider. Valid values: {validProviders}" });
        }

        try
        {
            await deleteKey.ExecuteAsync(new DeleteUserAiKey.Command(userId.Value, aiProvider), cancellationToken);
            return Results.NoContent();
        }
        catch (UserAiKeyNotFoundException)
        {
            return Results.NotFound();
        }
    }
}

public record UpsertAiKeyRequest([Required] string ApiKey, string? PreferredModel = null);
