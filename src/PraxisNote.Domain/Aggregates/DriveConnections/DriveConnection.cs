using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.DriveConnections;

/// <summary>
/// Represents a user's connection to Google Drive.
/// Stores OAuth tokens for accessing the Drive API on the user's behalf.
/// </summary>
public sealed class DriveConnection : AggregateRoot
{
    /// <summary>
    /// The user who owns this Drive connection.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The profile this Drive connection belongs to (data silo boundary).
    /// </summary>
    public Guid ProfileId { get; private set; }

    /// <summary>
    /// The Drive provider name (e.g., "Google").
    /// </summary>
    public string Provider { get; private init; } = null!;

    /// <summary>
    /// OAuth access token for the Drive API.
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
    /// When files were last synced from this connection.
    /// </summary>
    public DateTimeOffset? LastSyncedAt { get; private set; }

    /// <summary>
    /// The Google Drive folder ID to sync from. Set by the folder picker (#649).
    /// </summary>
    public string? FolderId { get; private set; }

    /// <summary>
    /// The display name of the linked folder. Set by the folder picker (#649).
    /// </summary>
    public string? FolderName { get; private set; }

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private DriveConnection() { }

    private DriveConnection(
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
    /// Creates a new Drive connection with OAuth tokens.
    /// </summary>
    public static DriveConnection Create(
        Guid userId,
        Guid profileId,
        string provider,
        string accessToken,
        string refreshToken,
        DateTimeOffset tokenExpiresAt)
    {
        return new DriveConnection(Guid.NewGuid(), userId, profileId, provider, accessToken, refreshToken, tokenExpiresAt);
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
    /// Sets the folder to sync from.
    /// </summary>
    public void SetFolder(string folderId, string folderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderId, nameof(folderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(folderName, nameof(folderName));

        FolderId = folderId;
        FolderName = folderName;
    }

    /// <summary>
    /// Clears the linked folder.
    /// </summary>
    public void ClearFolder()
    {
        FolderId = null;
        FolderName = null;
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

    /// <summary>
    /// Reassigns this Drive connection to a different user and profile.
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
