using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace PraxisNote.Web.Auth;

/// <summary>
/// Custom Google OAuth handler that stores the correlation ID in the encrypted
/// state parameter instead of a cookie. Firebase Hosting strips all cookies
/// except <c>__session</c> when proxying to Cloud Run, which breaks the default
/// cookie-based correlation used by <see cref="GoogleHandler"/>.
///
/// The state parameter is encrypted with ASP.NET Core Data Protection keys
/// (persisted to the database), so an attacker cannot forge or tamper with it.
/// This provides equivalent CSRF protection to the cookie approach.
/// </summary>
public class FirebaseProxyGoogleHandler(
    IOptionsMonitor<GoogleOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : GoogleHandler(options, logger, encoder)
{
    /// <summary>
    /// Stores the correlation ID only in the state parameter (via
    /// <see cref="AuthenticationProperties.Items"/>). Does NOT set a
    /// correlation cookie, since Firebase Hosting would strip it.
    /// </summary>
    protected override void GenerateCorrelationId(AuthenticationProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        var correlationId = Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        properties.Items[".xsrf"] = correlationId;
        // Intentionally skip setting a correlation cookie
    }

    /// <summary>
    /// Validates the correlation ID from the encrypted state parameter only.
    /// Skips cookie lookup because the cookie won't survive the Firebase proxy.
    /// </summary>
    protected override bool ValidateCorrelationId(AuthenticationProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        if (!properties.Items.TryGetValue(".xsrf", out var correlationId))
        {
            Logger.LogWarning("Correlation property '.xsrf' not found in state.");
            return false;
        }

        properties.Items.Remove(".xsrf");

        // The state parameter is encrypted with Data Protection keys,
        // so the correlation ID cannot be forged without the server's keys.
        return !string.IsNullOrEmpty(correlationId);
    }
}
