using System.Security.Claims;
using PraxisNote.Application.Features.ApiKeys;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class ApiKeyEndpoints
{
    public static void MapApiKeyEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/api-keys")
            .RequireAuthorization();

        group.MapGet("/", (Delegate)HandleGetApiKeys);
        group.MapPost("/", (Delegate)HandleCreateApiKey);
        group.MapDelete("/{id:guid}", (Delegate)HandleRevokeApiKey);
    }

    private static async Task<IResult> HandleGetApiKeys(
        ClaimsPrincipal user,
        GetUserApiKeys getUserApiKeys,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetUserApiKeys.Query(userId.Value);
        var keys = await getUserApiKeys.ExecuteAsync(query, cancellationToken);

        return Results.Ok(keys);
    }

    private static async Task<IResult> HandleCreateApiKey(
        HttpContext context,
        ClaimsPrincipal user,
        CreateApiKeyRequest request,
        CreateApiKey createApiKey,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Name is required" });
        }

        var profileId = context.GetProfileId();

        try
        {
            var command = new CreateApiKey.Command(userId.Value, profileId, request.Name, request.ExpiresAt);
            var result = await createApiKey.ExecuteAsync(command, cancellationToken);

            return Results.Created($"/api/api-keys/{result.ApiKeyId}", new
            {
                id = result.ApiKeyId,
                rawKey = result.RawKey,
                prefix = result.Prefix
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == CreateApiKey.TooManyKeysError)
        {
            return Results.Conflict(new { error = "Maximum number of API keys reached (5)" });
        }
    }

    private static async Task<IResult> HandleRevokeApiKey(
        Guid id,
        ClaimsPrincipal user,
        RevokeApiKey revokeApiKey,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new RevokeApiKey.Command(userId.Value, id);
        var success = await revokeApiKey.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }
}

public record CreateApiKeyRequest(string Name, DateTimeOffset? ExpiresAt = null);
