using PraxisNote.Domain.Aggregates.Profiles;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Middleware;

/// <summary>
/// Middleware that extracts the X-Profile-Id header for authenticated API requests,
/// validates the profile belongs to the user, and falls back to the user's default profile.
/// Sets the validated profile ID in HttpContext.Items for downstream use.
/// </summary>
public sealed class ProfileValidationMiddleware(RequestDelegate next)
{
    private const string ProfileIdHeader = "X-Profile-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to authenticated API and MCP requests
        if (!(context.Request.Path.StartsWithSegments("/api")
            || context.Request.Path.StartsWithSegments("/mcp"))
            || context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var userId = context.User.GetUserId();
        if (userId is null)
        {
            await next(context);
            return;
        }

        // If ProfileId was already set by the auth handler (e.g., API key auth), skip lookup
        if (context.Items.ContainsKey("ProfileId"))
        {
            await next(context);
            return;
        }

        // Resolve the profile repository from DI
        var profileRepository = context.RequestServices.GetRequiredService<IProfileRepository>();
        var cancellationToken = context.RequestAborted;

        Guid profileId;

        // Try to read X-Profile-Id header
        var hasHeader = context.Request.Headers.TryGetValue(ProfileIdHeader, out var headerValue);
        if (hasHeader)
        {
            if (!Guid.TryParse(headerValue.ToString(), out var requestedProfileId))
            {
                // Header present but not a valid GUID — return 400
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid X-Profile-Id header format" }, cancellationToken);
                return;
            }

            // Validate the requested profile belongs to this user
            var profile = await profileRepository.GetByIdAsync(requestedProfileId, cancellationToken);
            if (profile is not null && profile.UserId == userId.Value)
            {
                profileId = profile.Id;
            }
            else
            {
                // Invalid profile ID — return 403
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Profile not found or access denied" }, cancellationToken);
                return;
            }
        }
        else
        {
            // No header — fall back to default profile
            var defaultProfile = await profileRepository.GetDefaultByUserIdAsync(userId.Value, cancellationToken);
            if (defaultProfile is null)
            {
                // No default profile exists (should not happen after login creates one)
                // Return 400 to signal client needs to create a profile
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "No default profile found" }, cancellationToken);
                return;
            }

            profileId = defaultProfile.Id;
        }

        context.SetProfileId(profileId);
        await next(context);
    }
}
