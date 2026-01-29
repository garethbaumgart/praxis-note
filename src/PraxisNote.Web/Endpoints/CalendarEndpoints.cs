using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using PraxisNote.Application.Features.Calendar;

namespace PraxisNote.Web.Endpoints;

public static class CalendarEndpoints
{
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
        ClaimsPrincipal user,
        GetCalendarConnectionStatus getStatus,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await getStatus.ExecuteAsync(
            new GetCalendarConnectionStatus.Query(userId.Value),
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

        // Construct Google OAuth URL with calendar-specific scopes
        var authUrl = QueryHelpers.AddQueryString("https://accounts.google.com/o/oauth2/v2/auth", new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = callbackUrl,
            ["response_type"] = "code",
            ["scope"] = "https://www.googleapis.com/auth/calendar.events.readonly",
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["state"] = "calendar_connect"
        });

        return Results.Redirect(authUrl);
    }

    private static async Task<IResult> HandleGoogleCallback(
        HttpContext context,
        IConfiguration configuration,
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

        // Get user from cookie auth
        var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Results.Redirect("/settings?error=not_authenticated");
        }

        var clientId = configuration["Authentication:Google:ClientId"]!;
        var clientSecret = configuration["Authentication:Google:ClientSecret"]!;
        var callbackUrl = $"{context.Request.Scheme}://{context.Request.Host}/api/calendar/callback/google";

        // Exchange auth code for tokens
        using var httpClient = new HttpClient();
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
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

        var tokenData = JsonDocument.Parse(tokenJson);
        var accessToken = tokenData.RootElement.GetProperty("access_token").GetString()!;
        var refreshToken = tokenData.RootElement.TryGetProperty("refresh_token", out var rt)
            ? rt.GetString()
            : null;
        var expiresIn = tokenData.RootElement.GetProperty("expires_in").GetInt32();

        if (string.IsNullOrEmpty(refreshToken))
        {
            logger.LogError("No refresh token received from Google. User may need to revoke access and reconnect.");
            return Results.Redirect("/settings?error=no_refresh_token");
        }

        await connectCalendar.ExecuteAsync(
            new ConnectGoogleCalendar.Command(
                userId,
                accessToken,
                refreshToken,
                DateTimeOffset.UtcNow.AddSeconds(expiresIn)),
            cancellationToken);

        logger.LogInformation("User {UserId} connected Google Calendar", userId);
        return Results.Redirect("/settings?connected=true");
    }

    private static async Task<IResult> HandleSync(
        ClaimsPrincipal user,
        SyncCalendarEvents syncEvents,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        try
        {
            var result = await syncEvents.ExecuteAsync(
                new SyncCalendarEvents.Command(userId.Value),
                cancellationToken);

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> HandleDisconnect(
        ClaimsPrincipal user,
        DisconnectGoogleCalendar disconnectCalendar,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        await disconnectCalendar.ExecuteAsync(
            new DisconnectGoogleCalendar.Command(userId.Value),
            cancellationToken);

        return Results.Ok(new { message = "Calendar disconnected" });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var userId) ? userId : null;
    }
}
