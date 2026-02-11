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
        // Only apply to authenticated API requests
        if (!context.Request.Path.StartsWithSegments("/api")
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

        // Resolve the profile repository from DI
        var profileRepository = context.RequestServices.GetRequiredService<IProfileRepository>();

        Guid profileId;

        // Try to read X-Profile-Id header
        if (context.Request.Headers.TryGetValue(ProfileIdHeader, out var headerValue)
            && Guid.TryParse(headerValue.ToString(), out var requestedProfileId))
        {
            // Validate the requested profile belongs to this user
            var profile = await profileRepository.GetByIdAsync(requestedProfileId);
            if (profile is not null && profile.UserId == userId.Value)
            {
                profileId = profile.Id;
            }
            else
            {
                // Invalid profile ID — return 403
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Profile not found or access denied" });
                return;
            }
        }
        else
        {
            // No header — fall back to default profile
            var defaultProfile = await profileRepository.GetDefaultByUserIdAsync(userId.Value);
            if (defaultProfile is null)
            {
                // No default profile exists (should not happen after login creates one)
                // Return 400 to signal client needs to create a profile
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "No default profile found" });
                return;
            }

            profileId = defaultProfile.Id;
        }

        context.SetProfileId(profileId);
        await next(context);
    }
}
