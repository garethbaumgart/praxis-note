using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PraxisNote.Application.Features.Drive.Services;
using PraxisNote.Domain.Aggregates.DriveFileImports;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Tests.Drive;

public class DriveDeduplicationServiceTests
{
    private readonly IMeetingRepository _meetingRepository = Substitute.For<IMeetingRepository>();
    private readonly ILogger<DriveDeduplicationService> _logger = Substitute.For<ILogger<DriveDeduplicationService>>();
    private readonly DriveDeduplicationService _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();
    private readonly Guid _connectionId = Guid.NewGuid();

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DriveDeduplicationServiceTests()
    {
        _sut = new DriveDeduplicationService(_meetingRepository, _logger);
    }

    #region Helper Methods

    private DriveFileImport CreateParsedFileImport(
        string? title = "Team Standup",
        string? meetingDate = null,
        string? attendees = "Alice, Bob",
        string? transcript = "Meeting transcript content")
    {
        var import = DriveFileImport.Create(_connectionId, $"file-{Guid.NewGuid()}", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        var resultJson = JsonSerializer.Serialize(new
        {
            title,
            meetingDate = meetingDate ?? DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            attendees,
            summary = "Summary",
            transcript
        }, CamelCaseOptions);
        import.MarkParsed("content", resultJson);
        return import;
    }

    private Meeting CreateMeeting(
        string? title = "Team Standup",
        DateTimeOffset? meetingDate = null,
        string? attendees = "Alice, Bob",
        string? calendarEventId = null)
    {
        var meeting = calendarEventId is not null
            ? Meeting.CreateFromCalendar(_userId, _profileId, title, meetingDate ?? DateTimeOffset.UtcNow, attendees, calendarEventId)
            : Meeting.Create(_userId, _profileId, title, meetingDate ?? DateTimeOffset.UtcNow, attendees);
        return meeting;
    }

    private void SetupMeetings(params Meeting[] meetings)
    {
        _meetingRepository.GetRecentMeetingsForDedupAsync(
                _userId, _profileId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(meetings.ToList());
    }

    #endregion

    #region Layer 2: Calendar Event ID Tests

    [Fact]
    public async Task DeduplicateAsync_WithCalendarEventIdInContent_MatchesExistingMeeting()
    {
        // Arrange
        var meeting = CreateMeeting(calendarEventId: "abc-defg-hij");
        SetupMeetings(meeting);

        var fileImport = CreateParsedFileImport(
            transcript: "Meeting notes\nhttps://meet.google.com/abc-defg-hij\nDiscussion items");
        var parsedFiles = new List<DriveFileImport> { fileImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert
        Assert.Equal(DeduplicationType.CalendarEvent, fileImport.DuplicateType);
        Assert.Equal(1.0m, fileImport.DuplicateConfidence);
        Assert.Equal(meeting.Id, fileImport.MatchedMeetingId);
        Assert.Equal(meeting.Title, fileImport.DuplicateMatchTitle);
    }

    [Fact]
    public async Task DeduplicateAsync_WithCalendarEventIdInUrl_MatchesExistingMeeting()
    {
        // Arrange
        var meeting = CreateMeeting(calendarEventId: "event123abc");
        SetupMeetings(meeting);

        var fileImport = CreateParsedFileImport(
            transcript: "Meeting notes\nhttps://calendar.google.com/event?eid=event123abc\nDiscussion items");
        var parsedFiles = new List<DriveFileImport> { fileImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert
        Assert.Equal(DeduplicationType.CalendarEvent, fileImport.DuplicateType);
        Assert.Equal(1.0m, fileImport.DuplicateConfidence);
    }

    [Fact]
    public async Task DeduplicateAsync_WithNoCalendarEventId_SkipsLayer2()
    {
        // Arrange
        var meeting = CreateMeeting(
            title: "Completely Different Meeting",
            calendarEventId: "xyz-event-id");
        SetupMeetings(meeting);

        var fileImport = CreateParsedFileImport(
            title: "Team Standup",
            transcript: "No calendar links in this transcript");
        var parsedFiles = new List<DriveFileImport> { fileImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert — should not match on calendar event ID (different titles, no link match)
        Assert.NotEqual(DeduplicationType.CalendarEvent, fileImport.DuplicateType);
    }

    [Fact]
    public async Task DeduplicateAsync_WithCalendarEventId_NoMatchInDb_ProceedsToLayer3()
    {
        // Arrange — meeting has a different calendar event ID
        var meeting = CreateMeeting(
            title: "Team Standup",
            meetingDate: DateTimeOffset.UtcNow,
            attendees: "Alice, Bob",
            calendarEventId: "different-event-id");
        SetupMeetings(meeting);

        var fileImport = CreateParsedFileImport(
            title: "Team Standup",
            meetingDate: DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            attendees: "Alice, Bob",
            transcript: "Meeting notes\nhttps://meet.google.com/unmatched-id\nDiscussion");
        var parsedFiles = new List<DriveFileImport> { fileImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert — should fall through to Layer 3 fuzzy match
        Assert.Equal(DeduplicationType.FuzzyMatch, fileImport.DuplicateType);
    }

    #endregion

    #region Layer 3: Fuzzy Match Tests

    [Fact]
    public async Task DeduplicateAsync_WithExactTitleAndDateMatch_HighConfidence()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var meeting = CreateMeeting(title: "Team Standup", meetingDate: now, attendees: "Alice, Bob");
        SetupMeetings(meeting);

        var fileImport = CreateParsedFileImport(
            title: "Team Standup",
            meetingDate: now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            attendees: "Alice, Bob");
        var parsedFiles = new List<DriveFileImport> { fileImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert
        Assert.Equal(DeduplicationType.FuzzyMatch, fileImport.DuplicateType);
        Assert.True(fileImport.DuplicateConfidence >= 0.75m, $"Expected >= 0.75 but was {fileImport.DuplicateConfidence}");
        Assert.Equal(meeting.Title, fileImport.DuplicateMatchTitle);
    }

    [Fact]
    public async Task DeduplicateAsync_WithTitleContainsAndDateMatch_MediumConfidence()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var meeting = CreateMeeting(title: "Team Standup", meetingDate: now);
        SetupMeetings(meeting);

        var fileImport = CreateParsedFileImport(
            title: "Team Standup Notes",
            meetingDate: now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            attendees: null);
        var parsedFiles = new List<DriveFileImport> { fileImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert
        Assert.Equal(DeduplicationType.FuzzyMatch, fileImport.DuplicateType);
        Assert.True(fileImport.DuplicateConfidence >= 0.5m);
    }

    [Fact]
    public async Task DeduplicateAsync_WithTitleMatchOnly_LowConfidence()
    {
        // Arrange
        var meeting = CreateMeeting(
            title: "Team Standup",
            meetingDate: DateTimeOffset.UtcNow.AddDays(-30));
        SetupMeetings(meeting);

        var fileImport = CreateParsedFileImport(
            title: "Team Standup",
            meetingDate: DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            attendees: null);
        var parsedFiles = new List<DriveFileImport> { fileImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert — title matches (0.4) but date is far apart and no attendees, so below threshold
        Assert.Equal(DeduplicationType.None, fileImport.DuplicateType);
    }

    [Fact]
    public async Task DeduplicateAsync_WithTitleAndAttendeeOverlap_IncludesAttendeeScore()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var meeting = CreateMeeting(
            title: "Sprint Planning",
            meetingDate: now.AddDays(-5),
            attendees: "Alice, Bob, Charlie");
        SetupMeetings(meeting);

        var fileImport = CreateParsedFileImport(
            title: "Sprint Planning",
            meetingDate: now.AddDays(-5).AddHours(0.5).ToString("yyyy-MM-ddTHH:mm:sszzz"),
            attendees: "Alice, Bob, Charlie");
        var parsedFiles = new List<DriveFileImport> { fileImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert
        Assert.Equal(DeduplicationType.FuzzyMatch, fileImport.DuplicateType);
        // Title (0.4) + Date (0.35) + Attendees (0.25 * 1.0) = 1.0 capped
        Assert.True(fileImport.DuplicateConfidence >= 0.75m);
    }

    [Fact]
    public async Task DeduplicateAsync_WithNoTitleMatch_NoDuplicate()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var meeting = CreateMeeting(title: "Sprint Retrospective", meetingDate: now, attendees: "Alice, Bob");
        SetupMeetings(meeting);

        var fileImport = CreateParsedFileImport(
            title: "Product Roadmap Review",
            meetingDate: now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            attendees: "Alice, Bob");
        var parsedFiles = new List<DriveFileImport> { fileImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert
        Assert.Equal(DeduplicationType.None, fileImport.DuplicateType);
        Assert.Equal(0m, fileImport.DuplicateConfidence);
    }

    [Fact]
    public async Task DeduplicateAsync_WithDateOutsideProximity_LowerConfidence()
    {
        // Arrange
        var meeting = CreateMeeting(
            title: "Team Standup",
            meetingDate: DateTimeOffset.UtcNow.AddHours(-3),
            attendees: "Alice, Bob");
        SetupMeetings(meeting);

        var fileImport = CreateParsedFileImport(
            title: "Team Standup",
            meetingDate: DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            attendees: "Charlie, Dave");
        var parsedFiles = new List<DriveFileImport> { fileImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert — title only (0.4) is below threshold, date is outside 1h, no attendee overlap
        Assert.Equal(DeduplicationType.None, fileImport.DuplicateType);
    }

    [Fact]
    public async Task DeduplicateAsync_BelowThreshold_NoDuplicateFlagged()
    {
        // Arrange
        var meeting = CreateMeeting(
            title: "Team Standup",
            meetingDate: DateTimeOffset.UtcNow.AddDays(-10),
            attendees: "Zara, Xavier");
        SetupMeetings(meeting);

        var fileImport = CreateParsedFileImport(
            title: "Team Standup",
            meetingDate: DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            attendees: "Alice, Bob");
        var parsedFiles = new List<DriveFileImport> { fileImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert — title match (0.4) but date far off, no attendee overlap => below 0.5 threshold
        Assert.Equal(DeduplicationType.None, fileImport.DuplicateType);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task DeduplicateAsync_WithEmptyParsedFiles_ReturnsImmediately()
    {
        // Arrange
        var parsedFiles = new List<DriveFileImport>();

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert — should not query the database
        await _meetingRepository.DidNotReceive().GetRecentMeetingsForDedupAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeduplicateAsync_WithNullParsedResult_SkipsFile()
    {
        // Arrange
        var meeting = CreateMeeting(title: "Team Standup");
        SetupMeetings(meeting);

        var import = DriveFileImport.Create(_connectionId, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        import.MarkParsed("content", "invalid json {{{");
        var parsedFiles = new List<DriveFileImport> { import };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert — invalid JSON should be skipped gracefully
        Assert.Equal(DeduplicationType.None, import.DuplicateType);
    }

    [Fact]
    public async Task DeduplicateAsync_Layer2TakesPrecedenceOverLayer3()
    {
        // Arrange — meeting matches both Layer 2 (calendar ID) and Layer 3 (fuzzy)
        var now = DateTimeOffset.UtcNow;
        var meeting = CreateMeeting(
            title: "Team Standup",
            meetingDate: now,
            attendees: "Alice, Bob",
            calendarEventId: "abc-defg-hij");
        SetupMeetings(meeting);

        var fileImport = CreateParsedFileImport(
            title: "Team Standup",
            meetingDate: now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            attendees: "Alice, Bob",
            transcript: "Meeting notes\nhttps://meet.google.com/abc-defg-hij\nDiscussion");
        var parsedFiles = new List<DriveFileImport> { fileImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert — Layer 2 should win
        Assert.Equal(DeduplicationType.CalendarEvent, fileImport.DuplicateType);
        Assert.Equal(1.0m, fileImport.DuplicateConfidence);
    }

    [Fact]
    public async Task DeduplicateAsync_SkipsNonParsedFiles()
    {
        // Arrange
        var meeting = CreateMeeting(title: "Team Standup");
        SetupMeetings(meeting);

        var pendingImport = DriveFileImport.Create(_connectionId, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        // This file is still in Pending status — should be skipped
        var parsedFiles = new List<DriveFileImport> { pendingImport };

        // Act
        await _sut.DeduplicateAsync(_userId, _profileId, parsedFiles);

        // Assert
        Assert.Equal(DeduplicationType.None, pendingImport.DuplicateType);
    }

    #endregion

    #region Static Helper Tests

    [Fact]
    public void CalculateAttendeeOverlap_WithIdenticalAttendees_ReturnsOne()
    {
        var overlap = DriveDeduplicationService.CalculateAttendeeOverlap("Alice, Bob", "Alice, Bob");
        Assert.Equal(1.0m, overlap);
    }

    [Fact]
    public void CalculateAttendeeOverlap_WithPartialOverlap_ReturnsCorrectRatio()
    {
        // {alice, bob} vs {alice, charlie} → intersection=1, union=3 → 0.333...
        var overlap = DriveDeduplicationService.CalculateAttendeeOverlap("Alice, Bob", "Alice, Charlie");
        Assert.True(overlap > 0.3m && overlap < 0.4m, $"Expected ~0.33 but was {overlap}");
    }

    [Fact]
    public void CalculateAttendeeOverlap_WithNoOverlap_ReturnsZero()
    {
        var overlap = DriveDeduplicationService.CalculateAttendeeOverlap("Alice, Bob", "Charlie, Dave");
        Assert.Equal(0m, overlap);
    }

    [Fact]
    public void CalculateAttendeeOverlap_WithNullAttendees_ReturnsZero()
    {
        Assert.Equal(0m, DriveDeduplicationService.CalculateAttendeeOverlap(null, "Alice, Bob"));
        Assert.Equal(0m, DriveDeduplicationService.CalculateAttendeeOverlap("Alice, Bob", null));
    }

    [Fact]
    public void AreDatesWithinProximity_WithinOneHour_ReturnsTrue()
    {
        var date1 = DateTimeOffset.UtcNow;
        var date2 = date1.AddMinutes(30);
        Assert.True(DriveDeduplicationService.AreDatesWithinProximity(date1, date2));
    }

    [Fact]
    public void AreDatesWithinProximity_OutsideOneHour_ReturnsFalse()
    {
        var date1 = DateTimeOffset.UtcNow;
        var date2 = date1.AddHours(2);
        Assert.False(DriveDeduplicationService.AreDatesWithinProximity(date1, date2));
    }

    [Fact]
    public void AreDatesWithinProximity_WithNullDates_ReturnsFalse()
    {
        Assert.False(DriveDeduplicationService.AreDatesWithinProximity(null, DateTimeOffset.UtcNow));
        Assert.False(DriveDeduplicationService.AreDatesWithinProximity(DateTimeOffset.UtcNow, null));
    }

    [Fact]
    public void ExtractCalendarEventIds_WithMeetUrl_ExtractsId()
    {
        var ids = DriveDeduplicationService.ExtractCalendarEventIds(
            "Join at https://meet.google.com/abc-defg-hij");
        Assert.Contains("abc-defg-hij", ids);
    }

    [Fact]
    public void ExtractCalendarEventIds_WithCalendarUrl_ExtractsId()
    {
        var ids = DriveDeduplicationService.ExtractCalendarEventIds(
            "Event: https://calendar.google.com/event?eid=event123abc");
        Assert.Contains("event123abc", ids);
    }

    [Fact]
    public void ExtractCalendarEventIds_WithNoUrls_ReturnsEmpty()
    {
        var ids = DriveDeduplicationService.ExtractCalendarEventIds("No URLs here");
        Assert.Empty(ids);
    }

    [Fact]
    public void ExtractCalendarEventIds_WithNullContent_ReturnsEmpty()
    {
        var ids = DriveDeduplicationService.ExtractCalendarEventIds(null);
        Assert.Empty(ids);
    }

    #endregion
}
