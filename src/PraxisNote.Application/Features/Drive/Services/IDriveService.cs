namespace PraxisNote.Application.Features.Drive.Services;

/// <summary>
/// Abstraction for external Drive provider APIs.
/// </summary>
public interface IDriveService
{
    /// <summary>
    /// Lists folders accessible to the user in Google Drive.
    /// Returns top-level folders plus recently modified folders.
    /// </summary>
    Task<IReadOnlyList<DriveFolder>> ListFoldersAsync(
        string accessToken,
        string? searchQuery,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired access token using the refresh token.
    /// </summary>
    Task<TokenRefreshResult> RefreshAccessTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in a Drive folder, filtered by supported MIME types.
    /// Supports pagination via page tokens.
    /// </summary>
    Task<DriveFileListResult> ListFilesAsync(
        string accessToken,
        string folderId,
        DateTimeOffset? modifiedAfter,
        string? pageToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a binary file (e.g., .docx, .txt) from Google Drive.
    /// </summary>
    Task<Stream> DownloadFileAsync(
        string accessToken,
        string fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a Google Docs file as plain text.
    /// </summary>
    Task<string> ExportGoogleDocAsync(
        string accessToken,
        string fileId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A folder returned from an external Drive provider.
/// </summary>
public record DriveFolder(string Id, string Name, DateTimeOffset? ModifiedTime);

/// <summary>
/// Result of a token refresh operation.
/// Reuses the same shape as Calendar — same Google OAuth flow.
/// </summary>
public record TokenRefreshResult(string AccessToken, DateTimeOffset ExpiresAt, string? RefreshToken);

/// <summary>
/// A file returned from the Drive provider's file listing.
/// </summary>
public record DriveFile(string Id, string Name, string MimeType, DateTimeOffset? ModifiedTime);

/// <summary>
/// Paginated result of listing files in a Drive folder.
/// </summary>
public record DriveFileListResult(IReadOnlyList<DriveFile> Files, string? NextPageToken);
