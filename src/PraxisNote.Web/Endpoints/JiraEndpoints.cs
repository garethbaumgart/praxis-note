using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using PraxisNote.Application.Features.Jira;
using PraxisNote.Application.Features.Jira.Services;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static partial class JiraEndpoints
{
    private const string OAuthStateCookieName = ".JiraOAuthState";

    [GeneratedRegex(@"^[A-Z][A-Z0-9]+-\d+$")]
    private static partial Regex IssueKeyPattern();

    public static void MapJiraEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/jira")
            .RequireAuthorization();

        group.MapGet("/status", HandleGetStatus);
        group.MapGet("/connect", HandleConnect);
        group.MapGet("/callback", (Delegate)HandleCallback).AllowAnonymous();
        group.MapPost("/disconnect", (Delegate)HandleDisconnect);
        group.MapGet("/issue/{issueKey}", HandleResolveIssue);
    }

    private static async Task<IResult> HandleGetStatus(
        HttpContext context,
        ClaimsPrincipal user,
        GetJiraConnectionStatus getStatus,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        var profileId = context.GetProfileId();
        var result = await getStatus.ExecuteAsync(
            new GetJiraConnectionStatus.Query(userId.Value, profileId),
            cancellationToken);

        return Results.Ok(result);
    }

    private static IResult HandleConnect(
        HttpContext context,
        IConfiguration configuration)
    {
        var clientId = configuration["Jira:ClientId"];
        if (string.IsNullOrEmpty(clientId))
        {
            return Results.Problem("Jira OAuth is not configured.", statusCode: 503);
        }

        var request = context.Request;
        var callbackUrl = $"{request.Scheme}://{request.Host}/api/jira/callback";

        // Generate cryptographically random state for CSRF protection
        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        context.Response.Cookies.Append(OAuthStateCookieName, state, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10)
        });

        var authUrl = QueryHelpers.AddQueryString("https://auth.atlassian.com/authorize", new Dictionary<string, string?>
        {
            ["audience"] = "api.atlassian.com",
            ["client_id"] = clientId,
            ["scope"] = "read:jira-work offline_access",
            ["redirect_uri"] = callbackUrl,
            ["state"] = state,
            ["response_type"] = "code",
            ["prompt"] = "consent"
        });

        return Results.Redirect(authUrl);
    }

    private static async Task<IResult> HandleCallback(
        HttpContext context,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IJiraService jiraService,
        ConnectJira connectJira,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var code = context.Request.Query["code"].ToString();
        var error = context.Request.Query["error"].ToString();

        if (!string.IsNullOrEmpty(error))
        {
            logger.LogWarning("Jira OAuth error: {Error}", error);
            return Results.Redirect("/settings?error=jira_auth_denied");
        }

        if (string.IsNullOrEmpty(code))
        {
            return Results.Redirect("/settings?error=jira_no_code");
        }

        // Validate CSRF state
        var returnedState = context.Request.Query["state"].ToString();
        var expectedState = context.Request.Cookies[OAuthStateCookieName];
        context.Response.Cookies.Delete(OAuthStateCookieName);

        if (string.IsNullOrEmpty(expectedState) || !string.Equals(returnedState, expectedState, StringComparison.Ordinal))
        {
            logger.LogWarning("Jira OAuth state mismatch");
            return Results.Redirect("/settings?error=jira_auth_denied");
        }

        var userId = context.User.GetUserId();
        if (userId is null)
        {
            return Results.Redirect("/settings?error=not_authenticated");
        }

        var clientId = configuration["Jira:ClientId"]!;
        var clientSecret = configuration["Jira:ClientSecret"]!;
        var callbackUrl = $"{context.Request.Scheme}://{context.Request.Host}/api/jira/callback";

        // Exchange auth code for tokens
        using var httpClient = httpClientFactory.CreateClient();
        var tokenRequestBody = new
        {
            grant_type = "authorization_code",
            client_id = clientId,
            client_secret = clientSecret,
            code,
            redirect_uri = callbackUrl
        };

        var tokenResponse = await httpClient.PostAsJsonAsync("https://auth.atlassian.com/oauth/token", tokenRequestBody, cancellationToken);
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            logger.LogError("Jira token exchange failed: {Response}", tokenJson);
            return Results.Redirect("/settings?error=jira_token_exchange_failed");
        }

        string? accessToken;
        string? refreshToken;
        int expiresIn;

        try
        {
            using var tokenData = JsonDocument.Parse(tokenJson);
            accessToken = tokenData.RootElement.GetProperty("access_token").GetString();
            refreshToken = tokenData.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
            expiresIn = tokenData.RootElement.GetProperty("expires_in").GetInt32();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Jira token response");
            return Results.Redirect("/settings?error=jira_token_exchange_failed");
        }

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            logger.LogError("Missing access or refresh token from Jira");
            return Results.Redirect("/settings?error=jira_token_exchange_failed");
        }

        // Get accessible Jira Cloud resource
        var resource = await jiraService.GetAccessibleResourceAsync(accessToken, cancellationToken);
        if (resource is null)
        {
            logger.LogError("No accessible Jira resources found");
            return Results.Redirect("/settings?error=jira_no_resources");
        }

        var profileId = context.GetProfileId();

        await connectJira.ExecuteAsync(
            new ConnectJira.Command(
                userId.Value,
                profileId,
                resource.Value.CloudId,
                resource.Value.SiteUrl,
                accessToken,
                refreshToken,
                DateTimeOffset.UtcNow.AddSeconds(expiresIn)),
            cancellationToken);

        logger.LogInformation("User {UserId} connected Jira (site: {SiteUrl})", userId.Value, resource.Value.SiteUrl);
        return Results.Redirect("/settings?jira_connected=true");
    }

    private static async Task<IResult> HandleDisconnect(
        HttpContext context,
        ClaimsPrincipal user,
        DisconnectJira disconnectJira,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        var profileId = context.GetProfileId();
        await disconnectJira.ExecuteAsync(
            new DisconnectJira.Command(userId.Value, profileId),
            cancellationToken);

        return Results.Ok(new { message = "Jira disconnected" });
    }

    private static async Task<IResult> HandleResolveIssue(
        HttpContext context,
        ClaimsPrincipal user,
        string issueKey,
        ResolveJiraIssue resolveIssue,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        if (!IssueKeyPattern().IsMatch(issueKey))
        {
            return Results.BadRequest(new { error = "Invalid Jira issue key format. Expected: PROJECT-123" });
        }

        try
        {
            var profileId = context.GetProfileId();
            var result = await resolveIssue.ExecuteAsync(
                new ResolveJiraIssue.Query(userId.Value, profileId, issueKey),
                cancellationToken);

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
