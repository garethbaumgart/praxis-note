using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using PraxisNote.Infrastructure.Application.Users;

namespace PraxisNote.Web.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth");

        group.MapGet("/login/google", HandleGoogleLogin)
            .AllowAnonymous();

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
        CancellationToken cancellationToken)
    {
        var authenticateResult = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            return Results.Redirect("/?error=auth_failed");
        }

        var claims = authenticateResult.Principal.Claims.ToList();
        var providerId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var avatarUrl = claims.FirstOrDefault(c => c.Type == "picture")?.Value;

        if (string.IsNullOrEmpty(providerId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
        {
            return Results.Redirect("/?error=missing_claims");
        }

        var command = new LoginOrRegisterCommand(
            Provider: "Google",
            ProviderId: providerId,
            Email: email,
            Name: name,
            AvatarUrl: avatarUrl);

        var result = await loginOrRegister.ExecuteAsync(command, cancellationToken);

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name),
            new Claim("provider", "Google")
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        if (!string.IsNullOrEmpty(avatarUrl))
        {
            identity.AddClaim(new Claim("avatar_url", avatarUrl));
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
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = user.FindFirstValue(ClaimTypes.Email);
        var name = user.FindFirstValue(ClaimTypes.Name);
        var avatarUrl = user.FindFirstValue("avatar_url");
        var provider = user.FindFirstValue("provider");

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new UserDto(
            Guid.Parse(userId),
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
