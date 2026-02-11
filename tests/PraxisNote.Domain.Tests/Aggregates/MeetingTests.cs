using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Domain.Tests.Aggregates;

public class MeetingTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProfileId = Guid.NewGuid();
    private readonly string _validTitle = "Sprint Planning";
    private readonly string _validAttendees = "John, Sarah, Mike";

    #region Create Tests

    [Fact]
    public void Create_WithUserId_CreatesMeetingWithDefaults()
    {
        // Act
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Assert
        Assert.NotEqual(Guid.Empty, meeting.Id);
        Assert.Equal(_validUserId, meeting.UserId);
        Assert.Null(meeting.Title);
        Assert.NotNull(meeting.MeetingDate);
        Assert.Null(meeting.Attendees);
        Assert.Equal(MeetingStatus.Draft, meeting.Status);
    }

    [Fact]
    public void Create_WithTitleAndDate_CreatesMeetingWithValues()
    {
        // Arrange
        var meetingDate = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        var meeting = Meeting.Create(_validUserId, _validProfileId, _validTitle, meetingDate);

        // Assert
        Assert.Equal(_validTitle, meeting.Title);
        Assert.Equal(meetingDate, meeting.MeetingDate);
    }

    [Fact]
    public void Create_WithNullTitle_CreatesMeetingWithNullTitle()
    {
        // Act
        var meeting = Meeting.Create(_validUserId, _validProfileId, null);

        // Assert
        Assert.Null(meeting.Title);
    }

    [Fact]
    public void Create_WithWhitespaceTitle_TrimsToNull()
    {
        // Act
        var meeting = Meeting.Create(_validUserId, _validProfileId, "   ");

        // Assert
        Assert.Null(meeting.Title);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Meeting.Create(Guid.Empty, _validProfileId));
    }

    [Fact]
    public void Create_WithNullMeetingDate_DefaultsToCurrentTime()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var afterCreate = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(meeting.MeetingDate);
        Assert.InRange(meeting.MeetingDate.Value, beforeCreate, afterCreate);
    }

    [Fact]
    public void Create_WithNonUtcMeetingDate_ConvertsToUtc()
    {
        // Arrange - simulate a browser sending AEST (+10:00)
        var aestDate = new DateTimeOffset(2026, 2, 6, 10, 0, 0, TimeSpan.FromHours(10));

        // Act
        var meeting = Meeting.Create(_validUserId, _validProfileId, _validTitle, aestDate);

        // Assert - should be stored as UTC (offset 0)
        Assert.Equal(TimeSpan.Zero, meeting.MeetingDate!.Value.Offset);
        Assert.Equal(aestDate.UtcDateTime, meeting.MeetingDate.Value.UtcDateTime);
    }

    [Fact]
    public void Create_SetsCreatedAtAndUpdatedAtToSameValue()
    {
        // Act
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Assert
        Assert.Equal(meeting.CreatedAt, meeting.UpdatedAt);
    }

    #endregion

    #region UpdateTitle Tests

    [Fact]
    public void UpdateTitle_WithValidTitle_UpdatesTitle()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act
        meeting.UpdateTitle(_validTitle);

        // Assert
        Assert.Equal(_validTitle, meeting.Title);
    }

    [Fact]
    public void UpdateTitle_WithNull_SetsNullTitle()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId, _validTitle);

        // Act
        meeting.UpdateTitle(null);

        // Assert
        Assert.Null(meeting.Title);
    }

    [Fact]
    public void UpdateTitle_WithWhitespace_SetsNullTitle()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId, _validTitle);

        // Act
        meeting.UpdateTitle("   ");

        // Assert
        Assert.Null(meeting.Title);
    }

    [Fact]
    public void UpdateTitle_TrimsWhitespace()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act
        meeting.UpdateTitle("  Sprint Planning  ");

        // Assert
        Assert.Equal("Sprint Planning", meeting.Title);
    }

    [Fact]
    public void UpdateTitle_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.UpdateTitle(_validTitle);

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateTitle_WithSameTitle_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId, _validTitle);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.UpdateTitle(_validTitle);

        // Assert
        Assert.Equal(originalUpdatedAt, meeting.UpdatedAt);
    }

    #endregion

    #region UpdateMeetingDate Tests

    [Fact]
    public void UpdateMeetingDate_WithValidDate_UpdatesMeetingDate()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var newDate = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        meeting.UpdateMeetingDate(newDate);

        // Assert
        Assert.Equal(newDate, meeting.MeetingDate);
    }

    [Fact]
    public void UpdateMeetingDate_WithNull_SetsNull()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act
        meeting.UpdateMeetingDate(null);

        // Assert
        Assert.Null(meeting.MeetingDate);
    }

    [Fact]
    public void UpdateMeetingDate_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;
        var newDate = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        meeting.UpdateMeetingDate(newDate);

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateMeetingDate_WithNonUtcDate_ConvertsToUtc()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var aestDate = new DateTimeOffset(2026, 3, 15, 14, 30, 0, TimeSpan.FromHours(10));

        // Act
        meeting.UpdateMeetingDate(aestDate);

        // Assert - should be stored as UTC (offset 0)
        Assert.Equal(TimeSpan.Zero, meeting.MeetingDate!.Value.Offset);
        Assert.Equal(aestDate.UtcDateTime, meeting.MeetingDate.Value.UtcDateTime);
    }

    [Fact]
    public void UpdateMeetingDate_WithSameDate_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var meetingDate = DateTimeOffset.UtcNow.AddHours(1);
        var meeting = Meeting.Create(_validUserId, _validProfileId, _validTitle, meetingDate);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.UpdateMeetingDate(meetingDate);

        // Assert
        Assert.Equal(originalUpdatedAt, meeting.UpdatedAt);
    }

    #endregion

    #region UpdateAttendees Tests

    [Fact]
    public void UpdateAttendees_WithValidAttendees_UpdatesAttendees()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act
        meeting.UpdateAttendees(_validAttendees);

        // Assert
        Assert.Equal(_validAttendees, meeting.Attendees);
    }

    [Fact]
    public void UpdateAttendees_WithNull_SetsNull()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.UpdateAttendees(_validAttendees);

        // Act
        meeting.UpdateAttendees(null);

        // Assert
        Assert.Null(meeting.Attendees);
    }

    [Fact]
    public void UpdateAttendees_WithWhitespace_SetsNull()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act
        meeting.UpdateAttendees("   ");

        // Assert
        Assert.Null(meeting.Attendees);
    }

    [Fact]
    public void UpdateAttendees_TrimsWhitespace()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act
        meeting.UpdateAttendees("  John, Sarah  ");

        // Assert
        Assert.Equal("John, Sarah", meeting.Attendees);
    }

    [Fact]
    public void UpdateAttendees_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.UpdateAttendees(_validAttendees);

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateAttendees_WithSameAttendees_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.UpdateAttendees(_validAttendees);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.UpdateAttendees(_validAttendees);

        // Assert
        Assert.Equal(originalUpdatedAt, meeting.UpdatedAt);
    }

    #endregion

    #region UpdateStatus Tests

    [Theory]
    [InlineData(MeetingStatus.Draft)]
    [InlineData(MeetingStatus.Processing)]
    [InlineData(MeetingStatus.Ready)]
    [InlineData(MeetingStatus.Reviewed)]
    [InlineData(MeetingStatus.Failed)]
    public void UpdateStatus_WithValidStatus_UpdatesStatus(MeetingStatus newStatus)
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act
        meeting.UpdateStatus(newStatus);

        // Assert
        Assert.Equal(newStatus, meeting.Status);
    }

    [Fact]
    public void UpdateStatus_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.UpdateStatus(MeetingStatus.Processing);

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateStatus_WithSameStatus_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.UpdateStatus(MeetingStatus.Draft); // Same as initial status

        // Assert
        Assert.Equal(originalUpdatedAt, meeting.UpdatedAt);
    }

    #endregion

    #region MarkAsReviewed Tests

    [Fact]
    public void MarkAsReviewed_SetsStatusToReviewed()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.UpdateStatus(MeetingStatus.Ready);

        // Act
        meeting.MarkAsReviewed();

        // Assert
        Assert.Equal(MeetingStatus.Reviewed, meeting.Status);
    }

    [Fact]
    public void MarkAsReviewed_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.UpdateStatus(MeetingStatus.Ready);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.MarkAsReviewed();

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void MarkAsReviewed_WhenAlreadyReviewed_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.MarkAsReviewed();
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.MarkAsReviewed();

        // Assert
        Assert.Equal(originalUpdatedAt, meeting.UpdatedAt);
    }

    #endregion

    #region SubmitTranscript Tests

    [Fact]
    public void SubmitTranscript_WithValidTranscript_SetsTranscriptContent()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var transcript = "Speaker 1: Hello\nSpeaker 2: Hi there";

        // Act
        meeting.SubmitTranscript(transcript);

        // Assert
        Assert.Equal(transcript, meeting.TranscriptContent);
    }

    [Fact]
    public void SubmitTranscript_TrimsWhitespace()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act
        meeting.SubmitTranscript("  Transcript content  ");

        // Assert
        Assert.Equal("Transcript content", meeting.TranscriptContent);
    }

    [Fact]
    public void SubmitTranscript_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.SubmitTranscript("Transcript content");

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void SubmitTranscript_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            meeting.SubmitTranscript(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SubmitTranscript_WithEmptyOrWhitespace_ThrowsArgumentException(string invalidTranscript)
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            meeting.SubmitTranscript(invalidTranscript));
    }

    [Fact]
    public void SubmitTranscript_OverwritesExistingTranscript()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Original transcript");

        // Act
        meeting.SubmitTranscript("New transcript");

        // Assert
        Assert.Equal("New transcript", meeting.TranscriptContent);
    }

    #endregion

    #region ClearTranscript Tests

    [Fact]
    public void ClearTranscript_WithExistingTranscript_ClearsContent()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");

        // Act
        meeting.ClearTranscript();

        // Assert
        Assert.Null(meeting.TranscriptContent);
    }

    [Fact]
    public void ClearTranscript_WithExistingTranscript_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.ClearTranscript();

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void ClearTranscript_WithNoTranscript_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.ClearTranscript();

        // Assert
        Assert.Equal(originalUpdatedAt, meeting.UpdatedAt);
    }

    #endregion

    #region StartAnalysis Tests

    [Fact]
    public void StartAnalysis_WithTranscript_SetsProcessingStatus()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript content");

        // Act
        meeting.StartAnalysis();

        // Assert
        Assert.Equal(MeetingStatus.Processing, meeting.Status);
    }

    [Fact]
    public void StartAnalysis_WithoutTranscript_ThrowsInvalidOperationException()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            meeting.StartAnalysis());
    }

    [Fact]
    public void StartAnalysis_WithNullTranscript_ThrowsInvalidOperationException()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        // Transcript is null by default (can't submit empty - SubmitTranscript validates)

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            meeting.StartAnalysis());
    }

    #endregion

    #region CompleteAnalysis Tests

    [Fact]
    public void CompleteAnalysis_WithValidData_SetsAnalysisFieldsAndReadyStatus()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        var summary = "This was a productive meeting.";
        var keyPoints = "[\"Point 1\", \"Point 2\"]";
        var decisions = "[\"Decision 1\"]";

        // Act
        meeting.CompleteAnalysis(summary, keyPoints, decisions);

        // Assert
        Assert.Equal(MeetingStatus.Ready, meeting.Status);
        Assert.Equal(summary, meeting.Summary);
        Assert.Equal(keyPoints, meeting.KeyPoints);
        Assert.Equal(decisions, meeting.Decisions);
    }

    [Fact]
    public void CompleteAnalysis_WithNullSummary_ThrowsArgumentNullException()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act & Assert - ThrowIfNullOrWhiteSpace internally calls ThrowIfNull first for null values
        Assert.Throws<ArgumentNullException>(() =>
            meeting.CompleteAnalysis(null!, null, null));
    }

    [Fact]
    public void CompleteAnalysis_WithEmptySummary_ThrowsArgumentException()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            meeting.CompleteAnalysis("", null, null));
    }

    [Fact]
    public void CompleteAnalysis_WithWhitespaceSummary_ThrowsArgumentException()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            meeting.CompleteAnalysis("   ", null, null));
    }

    [Fact]
    public void CompleteAnalysis_TrimsSummary()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.CompleteAnalysis("  Summary with whitespace  ", null, null);

        // Assert
        Assert.Equal("Summary with whitespace", meeting.Summary);
    }

    [Fact]
    public void CompleteAnalysis_WithNullKeyPointsAndDecisions_Succeeds()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.CompleteAnalysis("Summary", null, null);

        // Assert
        Assert.Equal(MeetingStatus.Ready, meeting.Status);
        Assert.Null(meeting.KeyPoints);
        Assert.Null(meeting.Decisions);
    }

    [Fact]
    public void CompleteAnalysis_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.CompleteAnalysis("Summary", null, null);

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void CompleteAnalysis_WithBehavioralAnalysis_StoresData()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        var behavioralAnalysis = """{"speakingDynamics":{"talkTimeByParticipant":[{"participant":"John","percentage":60,"duration":"5:30"}]}}""";

        // Act
        meeting.CompleteAnalysis("Summary", null, null, behavioralAnalysis);

        // Assert
        Assert.Equal(MeetingStatus.Ready, meeting.Status);
        Assert.Equal(behavioralAnalysis, meeting.BehavioralAnalysis);
    }

    [Fact]
    public void CompleteAnalysis_WithNullBehavioralAnalysis_StoresNull()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.CompleteAnalysis("Summary", null, null, null);

        // Assert
        Assert.Equal(MeetingStatus.Ready, meeting.Status);
        Assert.Null(meeting.BehavioralAnalysis);
    }

    [Fact]
    public void CompleteAnalysis_WithoutBehavioralAnalysisParameter_DefaultsToNull()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.CompleteAnalysis("Summary", null, null);

        // Assert
        Assert.Null(meeting.BehavioralAnalysis);
    }

    #endregion

    #region FailAnalysis Tests

    [Fact]
    public void FailAnalysis_SetsFailedStatus()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.FailAnalysis();

        // Assert
        Assert.Equal(MeetingStatus.Failed, meeting.Status);
    }

    [Fact]
    public void FailAnalysis_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.FailAnalysis();

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    #endregion

    #region ClearAnalysis Tests

    [Fact]
    public void ClearAnalysis_WithExistingAnalysis_ClearsFieldsAndSetsDraftStatus()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Summary", "[\"Key Point\"]", "[\"Decision\"]");

        // Act
        meeting.ClearAnalysis();

        // Assert
        Assert.Equal(MeetingStatus.Draft, meeting.Status);
        Assert.Null(meeting.Summary);
        Assert.Null(meeting.KeyPoints);
        Assert.Null(meeting.Decisions);
    }

    [Fact]
    public void ClearAnalysis_ClearsBehavioralAnalysis()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        var behavioralAnalysis = """{"speakingDynamics":{}}""";
        meeting.CompleteAnalysis("Summary", null, null, behavioralAnalysis);

        // Act
        meeting.ClearAnalysis();

        // Assert
        Assert.Null(meeting.BehavioralAnalysis);
    }

    [Fact]
    public void ClearAnalysis_WithExistingAnalysis_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Summary", null, null);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.ClearAnalysis();

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void ClearAnalysis_WithNoAnalysis_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.ClearAnalysis();

        // Assert
        Assert.Equal(originalUpdatedAt, meeting.UpdatedAt);
    }

    #endregion

    #region Tag Management Tests

    [Fact]
    public void AddTag_WithValidTagId_AddsTagToMeeting()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var tagId = Guid.NewGuid();

        // Act
        meeting.AddTag(tagId);

        // Assert
        Assert.Contains(tagId, meeting.TagIds);
        Assert.Single(meeting.TagIds);
    }

    [Fact]
    public void AddTag_WithMultipleTags_AddsAllTags()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var tagId1 = Guid.NewGuid();
        var tagId2 = Guid.NewGuid();

        // Act
        meeting.AddTag(tagId1);
        meeting.AddTag(tagId2);

        // Assert
        Assert.Equal(2, meeting.TagIds.Count);
        Assert.Contains(tagId1, meeting.TagIds);
        Assert.Contains(tagId2, meeting.TagIds);
    }

    [Fact]
    public void AddTag_WithDuplicateTag_IsIdempotent()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var tagId = Guid.NewGuid();
        meeting.AddTag(tagId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.AddTag(tagId);

        // Assert
        Assert.Single(meeting.TagIds);
        Assert.Equal(originalUpdatedAt, meeting.UpdatedAt);
    }

    [Fact]
    public void AddTag_WithEmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            meeting.AddTag(Guid.Empty));
    }

    [Fact]
    public void AddTag_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;
        var tagId = Guid.NewGuid();

        // Act
        meeting.AddTag(tagId);

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void RemoveTag_WithExistingTag_RemovesTag()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var tagId = Guid.NewGuid();
        meeting.AddTag(tagId);

        // Act
        meeting.RemoveTag(tagId);

        // Assert
        Assert.Empty(meeting.TagIds);
    }

    [Fact]
    public void RemoveTag_WithNonExistentTag_IsIdempotent()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;
        var tagId = Guid.NewGuid();

        // Act
        meeting.RemoveTag(tagId);

        // Assert
        Assert.Empty(meeting.TagIds);
        Assert.Equal(originalUpdatedAt, meeting.UpdatedAt);
    }

    [Fact]
    public void RemoveTag_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var tagId = Guid.NewGuid();
        meeting.AddTag(tagId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.RemoveTag(tagId);

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void HasTag_WithExistingTag_ReturnsTrue()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var tagId = Guid.NewGuid();
        meeting.AddTag(tagId);

        // Act
        var result = meeting.HasTag(tagId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasTag_WithNonExistentTag_ReturnsFalse()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var tagId = Guid.NewGuid();

        // Act
        var result = meeting.HasTag(tagId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region StartTranscription Tests

    [Fact]
    public void StartTranscription_SetsProcessingStatus()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act
        meeting.StartTranscription();

        // Assert
        Assert.Equal(MeetingStatus.Processing, meeting.Status);
    }

    [Fact]
    public void StartTranscription_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.StartTranscription();

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void StartTranscription_DoesNotRequireTranscript()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act - should not throw (unlike StartAnalysis which requires transcript)
        meeting.StartTranscription();

        // Assert
        Assert.Equal(MeetingStatus.Processing, meeting.Status);
        Assert.Null(meeting.TranscriptContent);
    }

    #endregion

    #region CompleteTranscription Tests

    [Fact]
    public void CompleteTranscription_WithValidText_SetsTranscriptAndDraftStatus()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.StartTranscription();
        var transcribedText = "Speaker 1: Hello\nSpeaker 2: Hi there";

        // Act
        meeting.CompleteTranscription(transcribedText);

        // Assert
        Assert.Equal(transcribedText, meeting.TranscriptContent);
        Assert.Equal(MeetingStatus.Draft, meeting.Status);
    }

    [Fact]
    public void CompleteTranscription_TrimsWhitespace()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.StartTranscription();

        // Act
        meeting.CompleteTranscription("  Transcribed content  ");

        // Assert
        Assert.Equal("Transcribed content", meeting.TranscriptContent);
    }

    [Fact]
    public void CompleteTranscription_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.StartTranscription();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            meeting.CompleteTranscription(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CompleteTranscription_WithEmptyOrWhitespace_ThrowsArgumentException(string invalidText)
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.StartTranscription();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            meeting.CompleteTranscription(invalidText));
    }

    [Fact]
    public void CompleteTranscription_TransitionsFromProcessingToDraft()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.StartTranscription();
        Assert.Equal(MeetingStatus.Processing, meeting.Status);

        // Act
        meeting.CompleteTranscription("Transcribed text");

        // Assert
        Assert.Equal(MeetingStatus.Draft, meeting.Status);
    }

    [Fact]
    public void CompleteTranscription_AlwaysUpdatesUpdatedAt()
    {
        // Arrange - status is already Draft, so UpdateStatus would be a no-op
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.CompleteTranscription("Transcribed text");

        // Assert - UpdatedAt should still be updated even though status didn't change
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void CompleteTranscription_OverwritesExistingTranscript()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Old transcript");
        meeting.StartTranscription();

        // Act
        meeting.CompleteTranscription("New transcription from audio");

        // Assert
        Assert.Equal("New transcription from audio", meeting.TranscriptContent);
    }

    #endregion

    #region Action Item Tests

    [Fact]
    public void CompleteAnalysis_WithActionItems_StoresActionItems()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        var actionItems = new[]
        {
            ActionItem.Create("Send follow-up email", "John"),
            ActionItem.Create("Prepare presentation")
        };

        // Act
        meeting.CompleteAnalysis("Summary", null, null, null, actionItems);

        // Assert
        Assert.Equal(2, meeting.ActionItems.Count);
        Assert.Contains(meeting.ActionItems, a => a.Description == "Send follow-up email" && a.Assignee == "John");
        Assert.Contains(meeting.ActionItems, a => a.Description == "Prepare presentation" && a.Assignee == null);
    }

    [Fact]
    public void CompleteAnalysis_WithNullActionItems_HasEmptyList()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.CompleteAnalysis("Summary", null, null, null, null);

        // Assert
        Assert.Empty(meeting.ActionItems);
    }

    [Fact]
    public void CompleteAnalysis_OverwritesPreviousActionItems()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Summary", null, null, null, new[] { ActionItem.Create("Old item") });
        meeting.UpdateStatus(MeetingStatus.Draft);
        meeting.StartAnalysis();
        var newActionItems = new[] { ActionItem.Create("New item") };

        // Act
        meeting.CompleteAnalysis("New summary", null, null, null, newActionItems);

        // Assert
        Assert.Single(meeting.ActionItems);
        Assert.Equal("New item", meeting.ActionItems.First().Description);
    }

    [Fact]
    public void ToggleActionItem_WithExistingItem_TogglesCompletion()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        var actionItem = ActionItem.Create("Test item");
        meeting.CompleteAnalysis("Summary", null, null, null, new[] { actionItem });
        var itemId = meeting.ActionItems.First().Id;

        // Act
        var result = meeting.ToggleActionItem(itemId);

        // Assert
        Assert.True(result);
        Assert.True(meeting.ActionItems.First().IsCompleted);
    }

    [Fact]
    public void ToggleActionItem_TogglesBackToIncomplete()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        var actionItem = ActionItem.Create("Test item");
        meeting.CompleteAnalysis("Summary", null, null, null, new[] { actionItem });
        var itemId = meeting.ActionItems.First().Id;

        // Act - Toggle twice
        meeting.ToggleActionItem(itemId);
        meeting.ToggleActionItem(itemId);

        // Assert
        Assert.False(meeting.ActionItems.First().IsCompleted);
    }

    [Fact]
    public void ToggleActionItem_WithNonExistentItem_ReturnsFalse()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Summary", null, null, null, new[] { ActionItem.Create("Test item") });
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        var result = meeting.ToggleActionItem(Guid.NewGuid());

        // Assert
        Assert.False(result);
        Assert.Equal(originalUpdatedAt, meeting.UpdatedAt);
    }

    [Fact]
    public void ToggleActionItem_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Summary", null, null, null, new[] { ActionItem.Create("Test item") });
        var originalUpdatedAt = meeting.UpdatedAt;
        var itemId = meeting.ActionItems.First().Id;

        // Act
        meeting.ToggleActionItem(itemId);

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void ClearAnalysis_ClearsActionItems()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Summary", null, null, null, new[] { ActionItem.Create("Test item") });

        // Act
        meeting.ClearAnalysis();

        // Assert
        Assert.Empty(meeting.ActionItems);
    }

    [Fact]
    public void GetActionItem_WithExistingItem_ReturnsItem()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        var actionItem = ActionItem.Create("Test item");
        meeting.CompleteAnalysis("Summary", null, null, null, new[] { actionItem });
        var itemId = meeting.ActionItems.First().Id;

        // Act
        var result = meeting.GetActionItem(itemId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(itemId, result.Id);
        Assert.Equal("Test item", result.Description);
    }

    [Fact]
    public void GetActionItem_WithNonExistentItem_ReturnsNull()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Summary", null, null, null, new[] { ActionItem.Create("Test item") });

        // Act
        var result = meeting.GetActionItem(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetActionItem_WithEmptyActionItems_ReturnsNull()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Summary", null, null);

        // Act
        var result = meeting.GetActionItem(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Reflection Tests

    [Fact]
    public void SubmitReflection_WithValidJson_StoresReflectionData()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var json = """{"selfAssessedTalkTime":30,"selfAssessedEngagement":"medium"}""";

        // Act
        meeting.SubmitReflection(json);

        // Assert
        Assert.Equal(json, meeting.ReflectionData);
        Assert.NotNull(meeting.ReflectionSubmittedAt);
        Assert.True(meeting.HasReflection);
    }

    [Fact]
    public void SubmitReflection_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            meeting.SubmitReflection(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SubmitReflection_WithEmptyOrWhitespace_ThrowsArgumentException(string invalidJson)
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            meeting.SubmitReflection(invalidJson));
    }

    [Fact]
    public void SubmitReflection_TrimsWhitespace()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Act
        meeting.SubmitReflection("""  {"test":true}  """);

        // Assert
        Assert.Equal("""{"test":true}""", meeting.ReflectionData);
    }

    [Fact]
    public void SubmitReflection_UpdatesTimestamps()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var beforeSubmit = DateTimeOffset.UtcNow;

        // Act
        meeting.SubmitReflection("""{"test":true}""");

        // Assert
        Assert.True(meeting.ReflectionSubmittedAt >= beforeSubmit);
        Assert.True(meeting.UpdatedAt >= beforeSubmit);
    }

    [Fact]
    public void SubmitReflection_CalledTwice_OverwritesReflection()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitReflection("""{"first":true}""");

        // Act
        meeting.SubmitReflection("""{"second":true}""");

        // Assert
        Assert.Equal("""{"second":true}""", meeting.ReflectionData);
    }

    [Fact]
    public void HasReflection_WhenNoReflection_ReturnsFalse()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Assert
        Assert.False(meeting.HasReflection);
        Assert.Null(meeting.ReflectionData);
        Assert.Null(meeting.ReflectionSubmittedAt);
    }

    #endregion

    #region CreateFromCalendar Tests

    [Fact]
    public void CreateFromCalendar_WithValidInputs_CreatesMeetingWithCalendarEventId()
    {
        // Arrange
        var calendarEventId = "google_event_abc123";
        var meetingDate = DateTimeOffset.UtcNow.AddHours(2);

        // Act
        var meeting = Meeting.CreateFromCalendar(_validUserId, _validProfileId, _validTitle, meetingDate, _validAttendees, calendarEventId);

        // Assert
        Assert.NotEqual(Guid.Empty, meeting.Id);
        Assert.Equal(_validUserId, meeting.UserId);
        Assert.Equal(_validTitle, meeting.Title);
        Assert.Equal(meetingDate, meeting.MeetingDate);
        Assert.Equal(_validAttendees, meeting.Attendees);
        Assert.Equal(calendarEventId, meeting.CalendarEventId);
        Assert.Equal(MeetingStatus.Draft, meeting.Status);
    }

    [Fact]
    public void CreateFromCalendar_TrimsCalendarEventId()
    {
        // Act
        var meeting = Meeting.CreateFromCalendar(_validUserId, _validProfileId, _validTitle, null, null, "  event_123  ");

        // Assert
        Assert.Equal("event_123", meeting.CalendarEventId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateFromCalendar_WithInvalidCalendarEventId_ThrowsArgumentException(string? eventId)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Meeting.CreateFromCalendar(_validUserId, _validProfileId, _validTitle, null, null, eventId!));
    }

    [Fact]
    public void CreateFromCalendar_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Meeting.CreateFromCalendar(Guid.Empty, _validProfileId, _validTitle, null, null, "event_123"));
    }

    [Fact]
    public void Create_RegularMeeting_HasNullCalendarEventId()
    {
        // Verify regular Create method does not set CalendarEventId
        var meeting = Meeting.Create(_validUserId, _validProfileId, _validTitle);

        Assert.Null(meeting.CalendarEventId);
    }

    #endregion

    #region Auto-Generated Title Tests

    [Fact]
    public void CompleteAnalysis_WithSuggestedTitle_SetsTitle_WhenUntitled()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.CompleteAnalysis("Summary", null, null, suggestedTitle: "Q3 Budget Review with Sarah");

        // Assert
        Assert.Equal("Q3 Budget Review with Sarah", meeting.Title);
        Assert.True(meeting.IsTitleAutoGenerated);
    }

    [Fact]
    public void CompleteAnalysis_WithSuggestedTitle_DoesNotOverrideExistingTitle()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId, "My Custom Title");
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.CompleteAnalysis("Summary", null, null, suggestedTitle: "AI Suggested Title");

        // Assert
        Assert.Equal("My Custom Title", meeting.Title);
        Assert.False(meeting.IsTitleAutoGenerated);
    }

    [Fact]
    public void CompleteAnalysis_WithSuggestedTitle_SetsIsTitleAutoGenerated()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.CompleteAnalysis("Summary", null, null, suggestedTitle: "AI Title");

        // Assert
        Assert.True(meeting.IsTitleAutoGenerated);
    }

    [Fact]
    public void CompleteAnalysis_WithNullSuggestedTitle_DoesNotSetTitle()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.CompleteAnalysis("Summary", null, null, suggestedTitle: null);

        // Assert
        Assert.Null(meeting.Title);
        Assert.False(meeting.IsTitleAutoGenerated);
    }

    [Fact]
    public void CompleteAnalysis_WithWhitespaceSuggestedTitle_DoesNotSetTitle()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.CompleteAnalysis("Summary", null, null, suggestedTitle: "   ");

        // Assert
        Assert.Null(meeting.Title);
        Assert.False(meeting.IsTitleAutoGenerated);
    }

    [Fact]
    public void CompleteAnalysis_WithSuggestedTitle_TrimsSuggestedTitle()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.CompleteAnalysis("Summary", null, null, suggestedTitle: "  Trimmed Title  ");

        // Assert
        Assert.Equal("Trimmed Title", meeting.Title);
    }

    [Fact]
    public void UpdateTitle_ClearsIsTitleAutoGenerated()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Summary", null, null, suggestedTitle: "AI Title");
        Assert.True(meeting.IsTitleAutoGenerated);

        // Act
        meeting.UpdateTitle("User Edited Title");

        // Assert
        Assert.Equal("User Edited Title", meeting.Title);
        Assert.False(meeting.IsTitleAutoGenerated);
    }

    [Fact]
    public void Create_IsTitleAutoGenerated_DefaultsFalse()
    {
        // Act
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Assert
        Assert.False(meeting.IsTitleAutoGenerated);
    }

    #endregion

    #region ExcludeFromInsights Tests

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void SetExcludeFromInsights_SetsExcludedFlag(bool initial, bool target)
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SetExcludeFromInsights(initial);

        // Act
        meeting.SetExcludeFromInsights(target);

        // Assert
        Assert.Equal(target, meeting.ExcludeFromInsights);
    }

    [Fact]
    public void SetExcludeFromInsights_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.SetExcludeFromInsights(true);

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void SetExcludeFromInsights_WithSameValue_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.SetExcludeFromInsights(false); // Same as default

        // Assert
        Assert.Equal(originalUpdatedAt, meeting.UpdatedAt);
    }

    [Fact]
    public void ExcludeFromInsights_DefaultsFalse()
    {
        // Act
        var meeting = Meeting.Create(_validUserId, _validProfileId);

        // Assert
        Assert.False(meeting.ExcludeFromInsights);
    }

    #endregion

    #region Suggested Tags Tests

    [Fact]
    public void CompleteAnalysis_StoresSuggestedTags()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        var suggestedTags = """["budget","planning","review"]""";

        // Act
        meeting.CompleteAnalysis("Summary", null, null, suggestedTags: suggestedTags);

        // Assert
        Assert.Equal(suggestedTags, meeting.SuggestedTags);
    }

    [Fact]
    public void CompleteAnalysis_WithNullSuggestedTags_StoresNull()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();

        // Act
        meeting.CompleteAnalysis("Summary", null, null, suggestedTags: null);

        // Assert
        Assert.Null(meeting.SuggestedTags);
    }

    [Fact]
    public void ClearAnalysis_ClearsSuggestedTags()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validProfileId);
        meeting.SubmitTranscript("Some transcript");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Summary", null, null, suggestedTags: """["budget"]""");

        // Act
        meeting.ClearAnalysis();

        // Assert
        Assert.Null(meeting.SuggestedTags);
    }

    #endregion

    #region ActionItem Value Object Tests

    [Fact]
    public void ActionItem_Create_WithDescription_CreatesItem()
    {
        // Act
        var item = ActionItem.Create("Send email");

        // Assert
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal("Send email", item.Description);
        Assert.Null(item.Assignee);
        Assert.False(item.IsCompleted);
    }

    [Fact]
    public void ActionItem_Create_WithDescriptionAndAssignee_CreatesItem()
    {
        // Act
        var item = ActionItem.Create("Send email", "John");

        // Assert
        Assert.Equal("Send email", item.Description);
        Assert.Equal("John", item.Assignee);
    }

    [Fact]
    public void ActionItem_Create_TrimsDescription()
    {
        // Act
        var item = ActionItem.Create("  Send email  ");

        // Assert
        Assert.Equal("Send email", item.Description);
    }

    [Fact]
    public void ActionItem_Create_TrimsAssignee()
    {
        // Act
        var item = ActionItem.Create("Send email", "  John  ");

        // Assert
        Assert.Equal("John", item.Assignee);
    }

    [Fact]
    public void ActionItem_Create_WithWhitespaceAssignee_SetsNull()
    {
        // Act
        var item = ActionItem.Create("Send email", "   ");

        // Assert
        Assert.Null(item.Assignee);
    }

    [Fact]
    public void ActionItem_Create_WithNullDescription_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ActionItem.Create(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ActionItem_Create_WithEmptyDescription_ThrowsArgumentException(string description)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ActionItem.Create(description));
    }

    [Fact]
    public void ActionItem_WithCompletedToggled_ReturnsNewItemWithToggledState()
    {
        // Arrange
        var item = ActionItem.Create("Send email");

        // Act
        var toggledItem = item.WithCompletedToggled();

        // Assert
        Assert.True(toggledItem.IsCompleted);
        Assert.Equal(item.Id, toggledItem.Id);
        Assert.Equal(item.Description, toggledItem.Description);
    }

    [Fact]
    public void ActionItem_WithCompletedToggled_CanToggleBackToFalse()
    {
        // Arrange
        var item = ActionItem.Create("Send email");

        // Act
        var toggledOnce = item.WithCompletedToggled();
        var toggledTwice = toggledOnce.WithCompletedToggled();

        // Assert
        Assert.False(toggledTwice.IsCompleted);
    }

    #endregion
}
