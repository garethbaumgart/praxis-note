using Microsoft.Extensions.Options;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Calendar.Services;
using PraxisNote.Domain.Aggregates.CalendarConnections;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Calendar;

public sealed class SyncCalendarEvents(
    ICalendarConnectionRepository connectionRepository,
    IMeetingRepository meetingRepository,
    ICalendarService calendarService,
    IUnitOfWork unitOfWork,
    IOptions<GoogleCalendarSettings> settings)
{
    public record Command(Guid UserId);
    public record Result(int ImportedCount, int SkippedCount);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByUserIdAndProviderAsync(command.UserId, "Google", cancellationToken)
            ?? throw new InvalidOperationException("No Google Calendar connection found. Please connect your calendar first.");

        // Refresh token if expired
        if (connection.IsTokenExpired())
        {
            var refreshResult = await calendarService.RefreshAccessTokenAsync(connection.RefreshToken, cancellationToken);
            connection.UpdateTokens(refreshResult.AccessToken, refreshResult.ExpiresAt, refreshResult.RefreshToken);
        }

        // Fetch upcoming events
        var daysAhead = settings.Value.DefaultSyncDaysAhead;
        var events = await calendarService.GetUpcomingEventsAsync(connection.AccessToken, daysAhead, cancellationToken);

        if (events.Count == 0)
        {
            connection.RecordSync();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new Result(0, 0);
        }

        // Deduplicate against existing meetings
        var eventIds = events.Select(e => e.EventId).ToList();
        var existingIds = await meetingRepository.GetExistingCalendarEventIdsAsync(command.UserId, eventIds, cancellationToken);

        var imported = 0;
        var skipped = 0;

        foreach (var calendarEvent in events)
        {
            if (existingIds.Contains(calendarEvent.EventId))
            {
                skipped++;
                continue;
            }

            var meeting = Meeting.CreateFromCalendar(
                command.UserId,
                calendarEvent.Title,
                calendarEvent.StartTime,
                calendarEvent.Attendees,
                calendarEvent.EventId);

            await meetingRepository.AddAsync(meeting, cancellationToken);
            imported++;
        }

        connection.RecordSync();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(imported, skipped);
    }
}
