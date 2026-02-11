using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using PraxisNote.Application.Features.Calendar;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class CalendarEndpoints
{
    private const string OAuthStateCookieName = ".CalendarOAuthState";

    public static void MapCalendarEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/calendar")
            .RequireAuthorization();

        group.MapGet("/status", HandleGetStatus);
        group.MapGet("/connect/google", HandleConnectGoogle);
        group.MapGet("/callback/google", (Delegate)HandleGoogleCallback).AllowAnonymous();
        group.MapPost("/sync", (Delegate)HandleSync);
        group.MapPost("/disconnect", (Delegate)HandleDisconnect);
    }

    private static async Task<IResult> HandleGetStatus(
        HttpContext context,
        ClaimsPrincipal user,
        GetCalendarConnectionStatus getStatus,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        var profileId = context.GetProfileId();
        var result = await getStatus.ExecuteAsync(
            new GetCalendarConnectionStatus.Query(userId.Value, profileId),
            cancellationToken);

        return Results.Ok(result);
    }

    private static IResult HandleConnectGoogle(
        HttpContext context,
        IConfiguration configuration)
    {
        var clientId = configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrEmpty(clientId))
        {
            return Results.Problem("Google OAuth is not configured.", statusCode: 503);
        }

        // Build the redirect URI for the calendar OAuth callback
        var request = context.Request;
        var callbackUrl = $"{request.Scheme}://{request.Host}/api/calendar/callback/google";

        // Generate cryptographically random state for CSRF protection
        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        context.Response.Cookies.Append(OAuthStateCookieName, state, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10)
        });

        // Construct Google OAuth URL with calendar-specific scopes
        var authUrl = QueryHelpers.AddQueryString("https://accounts.google.com/o/oauth2/v2/auth", new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = callbackUrl,
            ["response_type"] = "code",
            ["scope"] = "https://www.googleapis.com/auth/calendar.events.readonly",
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["state"] = state
        });

        return Results.Redirect(authUrl);
    }

    private static async Task<IResult> HandleGoogleCallback(
        HttpContext context,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ConnectGoogleCalendar connectCalendar,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var code = context.Request.Query["code"].ToString();
        var error = context.Request.Query["error"].ToString();

        if (!string.IsNullOrEmpty(error))
        {
            logger.LogWarning("Google Calendar OAuth error: {Error}", error);
            return Results.Redirect("/settings?error=auth_denied");
        }

        if (string.IsNullOrEmpty(code))
        {
            return Results.Redirect("/settings?error=no_code");
        }

        // Validate CSRF state
        var returnedState = context.Request.Query["state"].ToString();
        var expectedState = context.Request.Cookies[OAuthStateCookieName];
        context.Response.Cookies.Delete(OAuthStateCookieName);

        if (string.IsNullOrEmpty(expectedState) || !string.Equals(returnedState, expectedState, StringComparison.Ordinal))
        {
            logger.LogWarning("OAuth state mismatch. Expected: {Expected}, Received: {Received}", expectedState, returnedState);
            return Results.Redirect("/settings?error=auth_denied");
        }

        // Get user from cookie auth
        var userId = context.User.GetUserId();
        if (userId is null)
        {
            return Results.Redirect("/settings?error=not_authenticated");
        }

        var clientId = configuration["Authentication:Google:ClientId"]!;
        var clientSecret = configuration["Authentication:Google:ClientSecret"]!;
        var callbackUrl = $"{context.Request.Scheme}://{context.Request.Host}/api/calendar/callback/google";

        // Exchange auth code for tokens
        using var httpClient = httpClientFactory.CreateClient();
        using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = callbackUrl,
            ["grant_type"] = "authorization_code"
        });

        var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest, cancellationToken);
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            logger.LogError("Google token exchange failed: {Response}", tokenJson);
            return Results.Redirect("/settings?error=token_exchange_failed");
        }

        string? accessToken;
        string? refreshToken;
        int expiresIn;

        try
        {
            using var tokenData = JsonDocument.Parse(tokenJson);
            accessToken = tokenData.RootElement.GetProperty("access_token").GetString();
            refreshToken = tokenData.RootElement.TryGetProperty("refresh_token", out var rt)
                ? rt.GetString()
                : null;
            expiresIn = tokenData.RootElement.GetProperty("expires_in").GetInt32();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Google token response");
            return Results.Redirect("/settings?error=token_exchange_failed");
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            logger.LogError("No access token received from Google");
            return Results.Redirect("/settings?error=token_exchange_failed");
        }

        if (string.IsNullOrEmpty(refreshToken))
        {
            logger.LogError("No refresh token received from Google. User may need to revoke access and reconnect.");
            return Results.Redirect("/settings?error=no_refresh_token");
        }

        // OAuth callback does not have the X-Profile-Id header, so use the default profile
        var profileId = context.GetProfileId();

        await connectCalendar.ExecuteAsync(
            new ConnectGoogleCalendar.Command(
                userId.Value,
                profileId,
                accessToken,
                refreshToken,
                DateTimeOffset.UtcNow.AddSeconds(expiresIn)),
            cancellationToken);

        logger.LogInformation("User {UserId} connected Google Calendar", userId.Value);
        return Results.Redirect("/settings?connected=true");
    }

    private static async Task<IResult> HandleSync(
        HttpContext context,
        ClaimsPrincipal user,
        SyncCalendarEvents syncEvents,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        try
        {
            var profileId = context.GetProfileId();
            var result = await syncEvents.ExecuteAsync(
                new SyncCalendarEvents.Command(userId.Value, profileId),
                cancellationToken);

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> HandleDisconnect(
        HttpContext context,
        ClaimsPrincipal user,
        DisconnectGoogleCalendar disconnectCalendar,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        var profileId = context.GetProfileId();
        await disconnectCalendar.ExecuteAsync(
            new DisconnectGoogleCalendar.Command(userId.Value, profileId),
            cancellationToken);

        return Results.Ok(new { message = "Calendar disconnected" });
    }
}
