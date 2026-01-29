namespace PraxisNote.Application.Features.Calendar.Services;

/// <summary>
/// Abstraction for external calendar provider APIs.
/// </summary>
public interface ICalendarService
{
    /// <summary>
    /// Fetches upcoming calendar events from the provider.
    /// </summary>
    Task<IReadOnlyList<CalendarEvent>> GetUpcomingEventsAsync(string accessToken, int daysAhead, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired access token using the refresh token.
    /// </summary>
    Task<TokenRefreshResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// A calendar event returned from an external provider.
/// </summary>
public record CalendarEvent(
    string EventId,
    string? Title,
    DateTimeOffset? StartTime,
    string? Attendees
);

/// <summary>
/// Result of a token refresh operation.
/// </summary>
public record TokenRefreshResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string? RefreshToken
);
