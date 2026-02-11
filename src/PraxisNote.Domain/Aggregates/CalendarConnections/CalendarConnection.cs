using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.CalendarConnections;

/// <summary>
/// Represents a user's connection to an external calendar provider (e.g., Google Calendar).
/// Stores OAuth tokens for accessing the calendar API on the user's behalf.
/// </summary>
public sealed class CalendarConnection : AggregateRoot
{
    /// <summary>
    /// The user who owns this calendar connection.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// The profile this calendar connection belongs to (data silo boundary).
    /// </summary>
    public Guid ProfileId { get; private init; }

    /// <summary>
    /// The calendar provider name (e.g., "Google").
    /// </summary>
    public string Provider { get; private init; } = null!;

    /// <summary>
    /// OAuth access token for the calendar API.
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
    /// When events were last synced from this connection.
    /// </summary>
    public DateTimeOffset? LastSyncedAt { get; private set; }

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private CalendarConnection() { }

    private CalendarConnection(
        Guid id,
        Guid userId,
        Guid profileId,
        string provider,
        string accessToken,
        string refreshToken,
        DateTimeOffset tokenExpiresAt) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty, nameof(profileId));
        ArgumentException.ThrowIfNullOrWhiteSpace(provider, nameof(provider));
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken, nameof(accessToken));
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken, nameof(refreshToken));

        UserId = userId;
        ProfileId = profileId;
        Provider = provider.Trim();
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiresAt = tokenExpiresAt;
        ConnectedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a new calendar connection with OAuth tokens.
    /// </summary>
    public static CalendarConnection Create(
        Guid userId,
        Guid profileId,
        string provider,
        string accessToken,
        string refreshToken,
        DateTimeOffset tokenExpiresAt)
    {
        return new CalendarConnection(Guid.NewGuid(), userId, profileId, provider, accessToken, refreshToken, tokenExpiresAt);
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
    /// Records that a sync was performed.
    /// </summary>
    public void RecordSync()
    {
        LastSyncedAt = DateTimeOffset.UtcNow;
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
}
