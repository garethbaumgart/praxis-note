using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PraxisNote.Application.Features.Calendar.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class GoogleCalendarService : ICalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;

    public GoogleCalendarService(IConfiguration configuration, ILogger<GoogleCalendarService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetUpcomingEventsAsync(
        string accessToken,
        int daysAhead,
        CancellationToken cancellationToken = default)
    {
        using var service = CreateCalendarService(accessToken);

        var request = service.Events.List("primary");
        request.TimeMinDateTimeOffset = DateTimeOffset.UtcNow;
        request.TimeMaxDateTimeOffset = DateTimeOffset.UtcNow.AddDays(daysAhead);
        request.SingleEvents = true;
        request.OrderBy = Google.Apis.Calendar.v3.EventsResource.ListRequest.OrderByEnum.StartTime;
        request.MaxResults = 100;

        var events = await request.ExecuteAsync(cancellationToken);

        var result = new List<CalendarEvent>();
        if (events.Items is null) return result;

        foreach (var evt in events.Items)
        {
            // Skip all-day events (no specific start time)
            if (evt.Start?.DateTimeDateTimeOffset is null)
                continue;

            var attendees = evt.Attendees?
                .Where(a => a.Email != null)
                .Select(a => a.DisplayName ?? a.Email!)
                .ToList();

            result.Add(new CalendarEvent(
                EventId: evt.Id,
                Title: evt.Summary ?? "(No title)",
                StartTime: evt.Start.DateTimeDateTimeOffset,
                Attendees: attendees is { Count: > 0 } ? string.Join(", ", attendees) : null
            ));
        }

        _logger.LogInformation("Fetched {Count} calendar events for next {Days} days", result.Count, daysAhead);
        return result;
    }

    public async Task<TokenRefreshResult> RefreshAccessTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var clientId = _configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Google OAuth ClientId not configured");
        var clientSecret = _configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Google OAuth ClientSecret not configured");

        using var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            }
        });

        var tokenResponse = await flow.RefreshTokenAsync("user", refreshToken, cancellationToken);

        _logger.LogInformation("Successfully refreshed Google Calendar access token");

        return new TokenRefreshResult(
            AccessToken: tokenResponse.AccessToken,
            ExpiresAt: DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600),
            RefreshToken: tokenResponse.RefreshToken
        );
    }

    private CalendarService CreateCalendarService(string accessToken)
    {
        var credential = GoogleCredential.FromAccessToken(accessToken);

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "PraxisNote"
        });
    }
}
