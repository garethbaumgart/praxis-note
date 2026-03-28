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

        // Model-only update: no API key provided
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            if (string.IsNullOrWhiteSpace(request.PreferredModel))
            {
                return Results.BadRequest(new { error = "Either apiKey or preferredModel must be provided" });
            }

            var modelCommand = new UpsertUserAiKey.Command(userId.Value, aiProvider, "", request.PreferredModel);
            try
            {
                await upsertKey.ExecuteAsync(modelCommand, cancellationToken);
            }
            catch (ArgumentException ex)
            {
                return Results.UnprocessableEntity(new { error = "invalid_model", message = ex.Message });
            }
            catch (UserAiKeyNotFoundException)
            {
                return Results.NotFound(new { error = "No key found for this provider" });
            }

            return Results.Ok(new { validated = true, rateLimited = false });
        }

        // Validate the key before persisting
        var validation = await validateKey.ExecuteAsync(
            new ValidateAiKey.Command(aiProvider, request.ApiKey), cancellationToken);

        if (!validation.Validated)
        {
            // Compensating delete — remove any previously stored key for this provider
            try
            {
                await deleteKey.ExecuteAsync(new DeleteUserAiKey.Command(userId.Value, aiProvider), cancellationToken);
            }
            catch (UserAiKeyNotFoundException) { /* No key stored — nothing to clean up */ }

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
            return Results.UnprocessableEntity(new { error = "invalid_model", message = ex.Message });
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

public record UpsertAiKeyRequest(string? ApiKey = null, string? PreferredModel = null);
