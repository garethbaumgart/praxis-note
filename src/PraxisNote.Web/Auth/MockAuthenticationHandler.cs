using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Features.Users;

namespace PraxisNote.Web.Auth;

public class MockAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "MockAuth";
    public const string HeaderName = "X-Mock-User";
}

public class MockAuthenticationHandler : AuthenticationHandler<MockAuthenticationOptions>
{
    private const string ProviderClaim = "provider";
    private readonly LoginOrRegisterUser _loginOrRegister;

    public MockAuthenticationHandler(
        IOptionsMonitor<MockAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        LoginOrRegisterUser loginOrRegister)
        : base(options, logger, encoder)
    {
        _loginOrRegister = loginOrRegister;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Only process if the mock header is present
        if (!Request.Headers.TryGetValue(MockAuthenticationOptions.HeaderName, out var headerValue))
        {
            return AuthenticateResult.NoResult();
        }

        var header = headerValue.ToString();
        if (string.IsNullOrEmpty(header))
        {
            return AuthenticateResult.NoResult();
        }

        // Parse header: email|name|userId
        var parts = header.Split('|');
        if (parts.Length < 3)
        {
            Logger.LogWarning("Invalid mock auth header format. Expected: email|name|userId");
            return AuthenticateResult.Fail("Invalid mock auth header format");
        }

        var email = parts[0];
        var name = parts[1];
        var userId = parts[2];

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(userId))
        {
            Logger.LogWarning("Mock auth header contains empty values");
            return AuthenticateResult.Fail("Mock auth header contains empty values");
        }

        // Register or get the mock user
        var command = new LoginOrRegisterCommand(
            Provider: "MockAuth",
            ProviderId: userId,
            Email: email,
            Name: name,
            AvatarUrl: null);

        var result = await _loginOrRegister.ExecuteAsync(command, Context.RequestAborted);

        Logger.LogDebug(
            "Mock authenticated user {UserId} ({Email}). IsNewUser: {IsNewUser}",
            result.UserId,
            email,
            result.IsNewUser);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name),
            new Claim(ProviderClaim, "MockAuth")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
