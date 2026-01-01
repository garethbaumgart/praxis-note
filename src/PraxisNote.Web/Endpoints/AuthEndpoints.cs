using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using PraxisNote.Infrastructure.Application.Users;

namespace PraxisNote.Web.Endpoints;

public static class AuthEndpoints
{
    private const string GooglePictureClaim = "picture";
    private const string AvatarUrlClaim = "avatar_url";
    private const string ProviderClaim = "provider";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth");

        group.MapGet("/login/google", HandleGoogleLogin)
            .AllowAnonymous();

        // Delegate cast required for async methods returning Task<IResult> in minimal APIs
        group.MapGet("/callback/google", (Delegate)HandleGoogleCallback)
            .AllowAnonymous();

        group.MapGet("/me", HandleGetCurrentUser)
            .RequireAuthorization();

        group.MapPost("/logout", (Delegate)HandleLogout)
            .RequireAuthorization();
    }

    private static IResult HandleGoogleLogin(HttpContext context)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/auth/callback/google"
        };

        return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> HandleGoogleCallback(
        HttpContext context,
        LoginOrRegisterUser loginOrRegister,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var authenticateResult = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            logger.LogWarning(
                "Google authentication failed. Failure message: {FailureMessage}",
                authenticateResult.Failure?.Message ?? "Unknown");
            return Results.Redirect("/?error=auth_failed");
        }

        var claims = authenticateResult.Principal.Claims.ToList();
        var providerId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var avatarUrl = claims.FirstOrDefault(c => c.Type == GooglePictureClaim)?.Value;

        if (string.IsNullOrEmpty(providerId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
        {
            logger.LogWarning(
                "Google authentication missing required claims. ProviderId: {HasProviderId}, Email: {HasEmail}, Name: {HasName}",
                !string.IsNullOrEmpty(providerId),
                !string.IsNullOrEmpty(email),
                !string.IsNullOrEmpty(name));
            return Results.Redirect("/?error=missing_claims");
        }

        var command = new LoginOrRegisterCommand(
            Provider: "Google",
            ProviderId: providerId,
            Email: email,
            Name: name,
            AvatarUrl: avatarUrl);

        var result = await loginOrRegister.ExecuteAsync(command, cancellationToken);

        logger.LogInformation(
            "User {UserId} logged in via Google. IsNewUser: {IsNewUser}",
            result.UserId,
            result.IsNewUser);

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name),
            new Claim(ProviderClaim, "Google")
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        if (!string.IsNullOrEmpty(avatarUrl))
        {
            identity.AddClaim(new Claim(AvatarUrlClaim, avatarUrl));
        }

        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });

        return Results.Redirect("/");
    }

    private static IResult HandleGetCurrentUser(ClaimsPrincipal user)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }

        var email = user.FindFirstValue(ClaimTypes.Email);
        var name = user.FindFirstValue(ClaimTypes.Name);
        var avatarUrl = user.FindFirstValue(AvatarUrlClaim);
        var provider = user.FindFirstValue(ProviderClaim);

        return Results.Ok(new UserDto(
            userId,
            email ?? "",
            name ?? "",
            avatarUrl,
            provider ?? ""));
    }

    private static async Task<IResult> HandleLogout(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok(new { message = "Logged out successfully" });
    }
}
