namespace PraxisNote.Application.Features.Jira.Services;

/// <summary>
/// Abstraction for the Jira Cloud REST API.
/// </summary>
public interface IJiraService
{
    /// <summary>
    /// Fetches a single Jira issue by key.
    /// </summary>
    Task<JiraIssueDto> GetIssueAsync(string cloudId, string issueKey, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired access token using the refresh token.
    /// </summary>
    Task<JiraTokenRefreshResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of accessible Jira Cloud resources for the authenticated user.
    /// Returns the first accessible resource's CloudId and site URL.
    /// </summary>
    Task<(string CloudId, string SiteUrl)?> GetAccessibleResourceAsync(string accessToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a Jira token refresh operation.
/// </summary>
public record JiraTokenRefreshResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string? RefreshToken);
