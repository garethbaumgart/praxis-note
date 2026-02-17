using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.JiraConnections;

/// <summary>
/// Represents a user's connection to a Jira Cloud instance via OAuth 2.0 (3LO).
/// Stores OAuth tokens for accessing the Jira API on the user's behalf.
/// </summary>
public sealed class JiraConnection : AggregateRoot
{
    /// <summary>
    /// The user who owns this Jira connection.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The profile this Jira connection belongs to (data silo boundary).
    /// </summary>
    public Guid ProfileId { get; private set; }

    /// <summary>
    /// The Atlassian Cloud ID for the connected Jira site.
    /// </summary>
    public string CloudId { get; private set; } = null!;

    /// <summary>
    /// The Jira site URL (e.g., "https://myorg.atlassian.net").
    /// </summary>
    public string SiteUrl { get; private set; } = null!;

    /// <summary>
    /// OAuth access token for the Jira API.
    /// </summary>
    public string AccessToken { get; private set; } = null!;

    /// <summary>
    /// OAuth refresh token for obtaining new access tokens.
    /// </summary>
    public string RefreshToken { get; private set; } = null!;

    /// <summary>
    /// When the access token expires.
    /// </summary>
    public DateTimeOffset TokenExpiresAt { get; private set; }

    /// <summary>
    /// When this connection was established.
    /// </summary>
    public DateTimeOffset ConnectedAt { get; private init; }

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private JiraConnection() { }

    private JiraConnection(
        Guid id,
        Guid userId,
        Guid profileId,
        string cloudId,
        string siteUrl,
        string accessToken,
        string refreshToken,
        DateTimeOffset tokenExpiresAt) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty, nameof(profileId));
        ArgumentException.ThrowIfNullOrWhiteSpace(cloudId, nameof(cloudId));
        ArgumentException.ThrowIfNullOrWhiteSpace(siteUrl, nameof(siteUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken, nameof(accessToken));
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken, nameof(refreshToken));

        UserId = userId;
        ProfileId = profileId;
        CloudId = cloudId.Trim();
        SiteUrl = siteUrl.Trim();
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiresAt = tokenExpiresAt;
        ConnectedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a new Jira connection with OAuth tokens.
    /// </summary>
    public static JiraConnection Create(
        Guid userId,
        Guid profileId,
        string cloudId,
        string siteUrl,
        string accessToken,
        string refreshToken,
        DateTimeOffset tokenExpiresAt)
    {
        return new JiraConnection(Guid.NewGuid(), userId, profileId, cloudId, siteUrl, accessToken, refreshToken, tokenExpiresAt);
    }

    /// <summary>
    /// Updates the OAuth tokens after a token refresh.
    /// </summary>
    public void UpdateTokens(string accessToken, DateTimeOffset tokenExpiresAt, string? refreshToken = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken, nameof(accessToken));

        AccessToken = accessToken;
        TokenExpiresAt = tokenExpiresAt;

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            RefreshToken = refreshToken;
        }
    }

    /// <summary>
    /// Checks if the access token has expired or is about to expire.
    /// </summary>
    /// <param name="bufferMinutes">Minutes before actual expiry to consider expired.</param>
    public bool IsTokenExpired(int bufferMinutes = 5)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bufferMinutes, nameof(bufferMinutes));
        return DateTimeOffset.UtcNow >= TokenExpiresAt.AddMinutes(-bufferMinutes);
    }

    /// <summary>
    /// Reassigns this Jira connection to a different user and profile.
    /// Used during account linking to transfer data before deleting the source user.
    /// </summary>
    public void Reassign(Guid newUserId, Guid newProfileId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(newUserId, Guid.Empty, nameof(newUserId));
        ArgumentOutOfRangeException.ThrowIfEqual(newProfileId, Guid.Empty, nameof(newProfileId));

        UserId = newUserId;
        ProfileId = newProfileId;
    }
}
