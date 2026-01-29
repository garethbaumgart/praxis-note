using Microsoft.Extensions.Options;
using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Calendar;
using PraxisNote.Application.Features.Calendar.Services;
using PraxisNote.Domain.Aggregates.CalendarConnections;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Tests.Calendar;

public class SyncCalendarEventsTests
{
    private readonly ICalendarConnectionRepository _connectionRepo = Substitute.For<ICalendarConnectionRepository>();
    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly ICalendarService _calendarService = Substitute.For<ICalendarService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IOptions<GoogleCalendarSettings> _settings = Options.Create(new GoogleCalendarSettings { DefaultSyncDaysAhead = 7 });

    private readonly SyncCalendarEvents _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public SyncCalendarEventsTests()
    {
        _sut = new SyncCalendarEvents(_connectionRepo, _meetingRepo, _calendarService, _unitOfWork, _settings);
    }

    private CalendarConnection CreateConnection(bool expired = false)
    {
        var expiresAt = expired
            ? DateTimeOffset.UtcNow.AddMinutes(-10)
            : DateTimeOffset.UtcNow.AddHours(1);

        return CalendarConnection.Create(_userId, "Google", "access-token", "refresh-token", expiresAt);
    }

    #region No Connection

    [Fact]
    public async Task ExecuteAsync_WithNoConnection_ThrowsInvalidOperationException()
    {
        _connectionRepo.GetByUserIdAndProviderAsync(_userId, "Google", Arg.Any<CancellationToken>())
            .Returns((CalendarConnection?)null);

        var command = new SyncCalendarEvents.Command(_userId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(command));
    }

    #endregion

    #region No Events

    [Fact]
    public async Task ExecuteAsync_WithNoEvents_ReturnsZeroCounts()
    {
        var connection = CreateConnection();
        _connectionRepo.GetByUserIdAndProviderAsync(_userId, "Google", Arg.Any<CancellationToken>())
            .Returns(connection);
        _calendarService.GetUpcomingEventsAsync("access-token", 7, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CalendarEvent>());

        var result = await _sut.ExecuteAsync(new SyncCalendarEvents.Command(_userId));

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Import New Events

    [Fact]
    public async Task ExecuteAsync_WithNewEvents_ImportsAll()
    {
        var connection = CreateConnection();
        _connectionRepo.GetByUserIdAndProviderAsync(_userId, "Google", Arg.Any<CancellationToken>())
            .Returns(connection);

        var events = new List<CalendarEvent>
        {
            new("evt-1", "Standup", DateTimeOffset.UtcNow.AddDays(1), "alice@test.com"),
            new("evt-2", "Retro", DateTimeOffset.UtcNow.AddDays(2), null)
        };
        _calendarService.GetUpcomingEventsAsync("access-token", 7, Arg.Any<CancellationToken>())
            .Returns(events);
        _meetingRepo.GetExistingCalendarEventIdsAsync(_userId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());

        var result = await _sut.ExecuteAsync(new SyncCalendarEvents.Command(_userId));

        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        await _meetingRepo.Received(2).AddAsync(Arg.Any<Meeting>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Deduplication

    [Fact]
    public async Task ExecuteAsync_WithExistingEvents_SkipsDuplicates()
    {
        var connection = CreateConnection();
        _connectionRepo.GetByUserIdAndProviderAsync(_userId, "Google", Arg.Any<CancellationToken>())
            .Returns(connection);

        var events = new List<CalendarEvent>
        {
            new("evt-1", "Standup", DateTimeOffset.UtcNow.AddDays(1), null),
            new("evt-2", "Retro", DateTimeOffset.UtcNow.AddDays(2), null),
            new("evt-3", "Planning", DateTimeOffset.UtcNow.AddDays(3), null)
        };
        _calendarService.GetUpcomingEventsAsync("access-token", 7, Arg.Any<CancellationToken>())
            .Returns(events);
        _meetingRepo.GetExistingCalendarEventIdsAsync(_userId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "evt-1", "evt-3" });

        var result = await _sut.ExecuteAsync(new SyncCalendarEvents.Command(_userId));

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(2, result.SkippedCount);
        await _meetingRepo.Received(1).AddAsync(Arg.Any<Meeting>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithAllExistingEvents_SkipsAll()
    {
        var connection = CreateConnection();
        _connectionRepo.GetByUserIdAndProviderAsync(_userId, "Google", Arg.Any<CancellationToken>())
            .Returns(connection);

        var events = new List<CalendarEvent>
        {
            new("evt-1", "Standup", DateTimeOffset.UtcNow.AddDays(1), null)
        };
        _calendarService.GetUpcomingEventsAsync("access-token", 7, Arg.Any<CancellationToken>())
            .Returns(events);
        _meetingRepo.GetExistingCalendarEventIdsAsync(_userId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "evt-1" });

        var result = await _sut.ExecuteAsync(new SyncCalendarEvents.Command(_userId));

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
        await _meetingRepo.DidNotReceive().AddAsync(Arg.Any<Meeting>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Token Refresh

    [Fact]
    public async Task ExecuteAsync_WithExpiredToken_RefreshesBeforeFetching()
    {
        var connection = CreateConnection(expired: true);
        _connectionRepo.GetByUserIdAndProviderAsync(_userId, "Google", Arg.Any<CancellationToken>())
            .Returns(connection);

        var newExpiry = DateTimeOffset.UtcNow.AddHours(1);
        _calendarService.RefreshAccessTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(new TokenRefreshResult("new-access-token", newExpiry, "new-refresh-token"));
        _calendarService.GetUpcomingEventsAsync("new-access-token", 7, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CalendarEvent>());

        var result = await _sut.ExecuteAsync(new SyncCalendarEvents.Command(_userId));

        await _calendarService.Received(1).RefreshAccessTokenAsync("refresh-token", Arg.Any<CancellationToken>());
        Assert.Equal(0, result.ImportedCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidToken_DoesNotRefresh()
    {
        var connection = CreateConnection(expired: false);
        _connectionRepo.GetByUserIdAndProviderAsync(_userId, "Google", Arg.Any<CancellationToken>())
            .Returns(connection);
        _calendarService.GetUpcomingEventsAsync("access-token", 7, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CalendarEvent>());

        await _sut.ExecuteAsync(new SyncCalendarEvents.Command(_userId));

        await _calendarService.DidNotReceive().RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Settings

    [Fact]
    public async Task ExecuteAsync_UsesConfiguredDaysAhead()
    {
        var customSettings = Options.Create(new GoogleCalendarSettings { DefaultSyncDaysAhead = 14 });
        var sut = new SyncCalendarEvents(_connectionRepo, _meetingRepo, _calendarService, _unitOfWork, customSettings);

        var connection = CreateConnection();
        _connectionRepo.GetByUserIdAndProviderAsync(_userId, "Google", Arg.Any<CancellationToken>())
            .Returns(connection);
        _calendarService.GetUpcomingEventsAsync("access-token", 14, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CalendarEvent>());

        await sut.ExecuteAsync(new SyncCalendarEvents.Command(_userId));

        await _calendarService.Received(1).GetUpcomingEventsAsync("access-token", 14, Arg.Any<CancellationToken>());
    }

    #endregion
}
