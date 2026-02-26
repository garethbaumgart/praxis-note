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
    /// Files older than this date are skipped during import.
    /// </summary>
    public DateOnly? InitialImportCutoffDate { get; private set; }

    /// <summary>
    /// How often to sync files from the linked folder (0 = manual only, 15, 30, 60).
    /// </summary>
    public int SyncFrequencyMinutes { get; private set; } = 15;

    /// <summary>
    /// Whether to automatically apply AI-suggested tags to imported files.
    /// </summary>
    public bool AutoAcceptTags { get; private set; }

    /// <summary>
    /// The user's IANA timezone (e.g., "America/New_York"). Used for AI parsing of Drive files.
    /// Stored on the connection during setup so background processing doesn't need the browser.
    /// </summary>
    public string? TimeZone { get; private set; }

    /// <summary>
    /// When the last background sync cycle completed (success or failure).
    /// </summary>
    public DateTimeOffset? LastSyncAt { get; private set; }

    /// <summary>
    /// Number of new files found in the last successful sync cycle.
    /// </summary>
    public int LastSyncFilesDiscovered { get; private set; }

    /// <summary>
    /// Number of files auto-imported in the last successful sync cycle.
    /// </summary>
    public int LastSyncFilesImported { get; private set; }

    /// <summary>
    /// Number of files queued for manual review in the last successful sync cycle.
    /// </summary>
    public int LastSyncFilesPendingReview { get; private set; }

    /// <summary>
    /// Number of files that errored during the last sync cycle.
    /// </summary>
    public int LastSyncFilesErrored { get; private set; }

    /// <summary>
    /// Error message from the last sync cycle, if the entire cycle failed.
    /// </summary>
    public string? LastSyncError { get; private set; }

    /// <summary>
    /// Number of consecutive sync failures. Sync pauses at 5.
    /// </summary>
    public int ConsecutiveFailures { get; private set; }

    /// <summary>
    /// Whether sync is paused due to repeated failures (max 5 consecutive).
    /// </summary>
    public bool IsSyncPaused => ConsecutiveFailures >= 5;

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
    /// Sets all configuration properties in one call: folder, cutoff date, sync frequency, and auto-accept tags.
    /// </summary>
    public void Configure(
        string folderId,
        string folderName,
        DateOnly? initialImportCutoffDate,
        int syncFrequencyMinutes,
        bool autoAcceptTags,
        string? timeZone = null)
    {
        // Validate all inputs before mutating state
        if (syncFrequencyMinutes != 0 && syncFrequencyMinutes != 15 && syncFrequencyMinutes != 30 && syncFrequencyMinutes != 60)
            throw new ArgumentOutOfRangeException(nameof(syncFrequencyMinutes), "Must be 0 (manual), 15, 30, or 60");

        SetFolder(folderId, folderName);
        InitialImportCutoffDate = initialImportCutoffDate;
        SyncFrequencyMinutes = syncFrequencyMinutes;
        AutoAcceptTags = autoAcceptTags;
        TimeZone = string.IsNullOrWhiteSpace(timeZone) ? null : timeZone.Trim();
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
    /// Records a completed sync cycle with results.
    /// </summary>
    public void RecordSyncResult(int filesDiscovered, int filesImported, int filesPendingReview, int filesErrored)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(filesDiscovered, nameof(filesDiscovered));
        ArgumentOutOfRangeException.ThrowIfNegative(filesImported, nameof(filesImported));
        ArgumentOutOfRangeException.ThrowIfNegative(filesPendingReview, nameof(filesPendingReview));
        ArgumentOutOfRangeException.ThrowIfNegative(filesErrored, nameof(filesErrored));

        LastSyncAt = DateTimeOffset.UtcNow;
        LastSyncFilesDiscovered = filesDiscovered;
        LastSyncFilesImported = filesImported;
        LastSyncFilesPendingReview = filesPendingReview;
        LastSyncFilesErrored = filesErrored;
        LastSyncError = null;
        ConsecutiveFailures = filesErrored > 0 && filesImported == 0 && filesPendingReview == 0
            ? ConsecutiveFailures + 1
            : 0;
    }

    /// <summary>
    /// Records a sync-level failure (e.g., OAuth expired, folder not found).
    /// </summary>
    public void RecordSyncFailure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage, nameof(errorMessage));

        LastSyncAt = DateTimeOffset.UtcNow;
        LastSyncError = errorMessage.Trim();
        ConsecutiveFailures++;
    }

    /// <summary>
    /// Whether the connection is due for a sync based on configured frequency.
    /// Manual-only connections (SyncFrequencyMinutes == 0) are never due.
    /// </summary>
    public bool IsDueForSync()
    {
        if (SyncFrequencyMinutes == 0) return false;
        if (LastSyncAt is null) return true;
        return DateTimeOffset.UtcNow - LastSyncAt.Value >= TimeSpan.FromMinutes(SyncFrequencyMinutes);
    }

    /// <summary>
    /// Clear error state when user manually reconnects or triggers sync.
    /// </summary>
    public void ClearSyncError()
    {
        LastSyncError = null;
        ConsecutiveFailures = 0;
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
