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
