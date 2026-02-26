using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Domain.Aggregates.DriveFileImports;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class DriveEndpoints
{
    private const string OAuthStateCookieName = ".DriveOAuthState";

    public static void MapDriveEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/drive")
            .RequireAuthorization();

        group.MapGet("/status", HandleGetStatus);
        group.MapGet("/connect/google", HandleConnectGoogle);
        group.MapGet("/callback/google", (Delegate)HandleGoogleCallback).AllowAnonymous();
        group.MapPost("/disconnect", (Delegate)HandleDisconnect);
        group.MapGet("/folders", HandleListFolders);
        group.MapPut("/settings", HandleUpdateSettings);
        group.MapPost("/discover", HandleDiscover);
        group.MapGet("/files", HandleListFiles);
    }

    private static async Task<IResult> HandleGetStatus(
        HttpContext context,
        ClaimsPrincipal user,
        GetDriveConnectionStatus getStatus,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        var profileId = context.GetProfileId();
        var result = await getStatus.ExecuteAsync(
            new GetDriveConnectionStatus.Query(userId.Value, profileId),
            cancellationToken);

        return Results.Ok(result);
    }

    private static IResult HandleConnectGoogle(
        HttpContext context,
        IConfiguration configuration)
    {
        var clientId = configuration["Authentication:Google:ClientId"];
        var clientSecret = configuration["Authentication:Google:ClientSecret"];
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return Results.Problem("Google OAuth is not configured.", statusCode: 503);
        }

        // Build the redirect URI for the Drive OAuth callback
        var request = context.Request;
        var callbackUrl = $"{request.Scheme}://{request.Host}/api/drive/callback/google";

        // Generate cryptographically random, URL-safe state for CSRF protection
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        context.Response.Cookies.Append(OAuthStateCookieName, state, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10)
        });

        // Construct Google OAuth URL with Drive-specific scopes
        var authUrl = QueryHelpers.AddQueryString("https://accounts.google.com/o/oauth2/v2/auth", new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = callbackUrl,
            ["response_type"] = "code",
            ["scope"] = "https://www.googleapis.com/auth/drive.readonly",
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
        ConnectGoogleDrive connectDrive,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var code = context.Request.Query["code"].ToString();
        var error = context.Request.Query["error"].ToString();

        if (!string.IsNullOrEmpty(error))
        {
            logger.LogWarning("Google Drive OAuth error: {Error}", error);
            return Results.Redirect("/settings?error=drive_auth_denied");
        }

        if (string.IsNullOrEmpty(code))
        {
            return Results.Redirect("/settings?error=drive_no_code");
        }

        // Validate CSRF state
        var returnedState = context.Request.Query["state"].ToString();
        var expectedState = context.Request.Cookies[OAuthStateCookieName];
        context.Response.Cookies.Delete(OAuthStateCookieName);

        if (string.IsNullOrEmpty(expectedState) || !string.Equals(returnedState, expectedState, StringComparison.Ordinal))
        {
            logger.LogWarning("Drive OAuth state mismatch.");
            return Results.Redirect("/settings?error=drive_auth_denied");
        }

        // Get user from cookie auth
        var userId = context.User.GetUserId();
        if (userId is null)
        {
            return Results.Redirect("/settings?error=not_authenticated");
        }

        var clientId = configuration["Authentication:Google:ClientId"];
        var clientSecret = configuration["Authentication:Google:ClientSecret"];
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            logger.LogError("Google OAuth credentials are not configured.");
            return Results.Redirect("/settings?error=drive_token_exchange_failed");
        }
        var callbackUrl = $"{context.Request.Scheme}://{context.Request.Host}/api/drive/callback/google";

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
            logger.LogError("Google Drive token exchange failed with status {StatusCode}", tokenResponse.StatusCode);
            return Results.Redirect("/settings?error=drive_token_exchange_failed");
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
            logger.LogError(ex, "Failed to parse Google Drive token response");
            return Results.Redirect("/settings?error=drive_token_exchange_failed");
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            logger.LogError("No access token received from Google Drive");
            return Results.Redirect("/settings?error=drive_token_exchange_failed");
        }

        if (string.IsNullOrEmpty(refreshToken))
        {
            logger.LogError("No refresh token received from Google Drive. User may need to revoke access and reconnect.");
            return Results.Redirect("/settings?error=drive_no_refresh_token");
        }

        // OAuth callback does not have the X-Profile-Id header, so use the default profile
        var profileId = context.GetProfileId();

        await connectDrive.ExecuteAsync(
            new ConnectGoogleDrive.Command(
                userId.Value,
                profileId,
                accessToken,
                refreshToken,
                DateTimeOffset.UtcNow.AddSeconds(expiresIn)),
            cancellationToken);

        logger.LogInformation("User {UserId} connected Google Drive", userId.Value);
        return Results.Redirect("/settings?drive_connected=true");
    }

    private static async Task<IResult> HandleDisconnect(
        HttpContext context,
        ClaimsPrincipal user,
        DisconnectGoogleDrive disconnectDrive,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        var profileId = context.GetProfileId();
        await disconnectDrive.ExecuteAsync(
            new DisconnectGoogleDrive.Command(userId.Value, profileId),
            cancellationToken);

        return Results.Ok(new { message = "Drive disconnected" });
    }

    private static async Task<IResult> HandleListFolders(
        HttpContext context,
        ClaimsPrincipal user,
        ListDriveFolders listFolders,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        var profileId = context.GetProfileId();
        var result = await listFolders.ExecuteAsync(
            new ListDriveFolders.Query(userId.Value, profileId, search),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdateSettings(
        HttpContext context,
        ClaimsPrincipal user,
        UpdateDriveSettings updateSettings,
        UpdateDriveSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        var profileId = context.GetProfileId();
        await updateSettings.ExecuteAsync(
            new UpdateDriveSettings.Command(
                userId.Value,
                profileId,
                request.FolderId,
                request.FolderName,
                request.InitialImportCutoffDate,
                request.SyncFrequencyMinutes,
                request.AutoAcceptTags),
            cancellationToken);

        return Results.Ok(new { message = "Settings saved" });
    }

    private static async Task<IResult> HandleDiscover(
        HttpContext context,
        ClaimsPrincipal user,
        DiscoverDriveFiles discoverFiles,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        try
        {
            var profileId = context.GetProfileId();
            var result = await discoverFiles.ExecuteAsync(
                new DiscoverDriveFiles.Command(userId.Value, profileId),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> HandleListFiles(
        HttpContext context,
        ClaimsPrincipal user,
        GetDriveFileImports getFiles,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        DriveFileImportStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<DriveFileImportStatus>(status, true, out var parsed))
                return Results.BadRequest(new { error = $"Invalid status '{status}'." });
            statusFilter = parsed;
        }

        try
        {
            var profileId = context.GetProfileId();
            var result = await getFiles.ExecuteAsync(
                new GetDriveFileImports.Query(userId.Value, profileId, statusFilter),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    public record UpdateDriveSettingsRequest(
        string FolderId,
        string FolderName,
        DateOnly? InitialImportCutoffDate,
        int SyncFrequencyMinutes,
        bool AutoAcceptTags);
}
