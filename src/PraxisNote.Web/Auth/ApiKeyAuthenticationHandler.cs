using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.ApiKeys;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Auth;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ApiKey";
    public const string BearerPrefix = "Bearer pn_";
}

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyRepository apiKeyRepository,
    IUnitOfWork unitOfWork)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Bearer pn_", StringComparison.Ordinal))
            return AuthenticateResult.NoResult();

        var rawKey = headerValue["Bearer ".Length..];
        var keyHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawKey))).ToLowerInvariant();

        var apiKey = await apiKeyRepository.GetByKeyHashAsync(keyHash, Context.RequestAborted);
        if (apiKey is null || !apiKey.IsValid)
            return AuthenticateResult.Fail("Invalid API key");

        try
        {
            if (apiKey.LastUsedAt is null || apiKey.LastUsedAt.Value < DateTimeOffset.UtcNow.AddMinutes(-5))
            {
                apiKey.RecordUsage();
                await unitOfWork.SaveChangesAsync(Context.RequestAborted);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to record API key usage for key {KeyId}", apiKey.Id);
        }

        Context.SetProfileId(apiKey.ProfileId);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, apiKey.UserId.ToString()),
            new Claim("provider", "ApiKey"),
            new Claim("api_key_id", apiKey.Id.ToString()),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
