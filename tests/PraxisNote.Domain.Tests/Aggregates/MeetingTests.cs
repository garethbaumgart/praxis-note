using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Domain.Tests.Aggregates;

public class MeetingTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly string _validTitle = "Sprint Planning";
    private readonly string _validAttendees = "John, Sarah, Mike";

    #region Create Tests

    [Fact]
    public void Create_WithUserId_CreatesMeetingWithDefaults()
    {
        // Act
        var meeting = Meeting.Create(_validUserId);

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
        var meeting = Meeting.Create(_validUserId, _validTitle, meetingDate);

        // Assert
        Assert.Equal(_validTitle, meeting.Title);
        Assert.Equal(meetingDate, meeting.MeetingDate);
    }

    [Fact]
    public void Create_WithNullTitle_CreatesMeetingWithNullTitle()
    {
        // Act
        var meeting = Meeting.Create(_validUserId, null);

        // Assert
        Assert.Null(meeting.Title);
    }

    [Fact]
    public void Create_WithWhitespaceTitle_TrimsToNull()
    {
        // Act
        var meeting = Meeting.Create(_validUserId, "   ");

        // Assert
        Assert.Null(meeting.Title);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Meeting.Create(Guid.Empty));
    }

    [Fact]
    public void Create_WithNullMeetingDate_DefaultsToCurrentTime()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var meeting = Meeting.Create(_validUserId);
        var afterCreate = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(meeting.MeetingDate);
        Assert.InRange(meeting.MeetingDate.Value, beforeCreate, afterCreate);
    }

    [Fact]
    public void Create_SetsCreatedAtAndUpdatedAtToSameValue()
    {
        // Act
        var meeting = Meeting.Create(_validUserId);

        // Assert
        Assert.Equal(meeting.CreatedAt, meeting.UpdatedAt);
    }

    #endregion

    #region UpdateTitle Tests

    [Fact]
    public void UpdateTitle_WithValidTitle_UpdatesTitle()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId);

        // Act
        meeting.UpdateTitle(_validTitle);

        // Assert
        Assert.Equal(_validTitle, meeting.Title);
    }

    [Fact]
    public void UpdateTitle_WithNull_SetsNullTitle()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validTitle);

        // Act
        meeting.UpdateTitle(null);

        // Assert
        Assert.Null(meeting.Title);
    }

    [Fact]
    public void UpdateTitle_WithWhitespace_SetsNullTitle()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId, _validTitle);

        // Act
        meeting.UpdateTitle("   ");

        // Assert
        Assert.Null(meeting.Title);
    }

    [Fact]
    public void UpdateTitle_TrimsWhitespace()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId);

        // Act
        meeting.UpdateTitle("  Sprint Planning  ");

        // Assert
        Assert.Equal("Sprint Planning", meeting.Title);
    }

    [Fact]
    public void UpdateTitle_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId, _validTitle);
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
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);

        // Act
        meeting.UpdateMeetingDate(null);

        // Assert
        Assert.Null(meeting.MeetingDate);
    }

    [Fact]
    public void UpdateMeetingDate_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId);
        var originalUpdatedAt = meeting.UpdatedAt;
        var newDate = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        meeting.UpdateMeetingDate(newDate);

        // Assert
        Assert.True(meeting.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateMeetingDate_WithSameDate_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var meetingDate = DateTimeOffset.UtcNow.AddHours(1);
        var meeting = Meeting.Create(_validUserId, _validTitle, meetingDate);
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
        var meeting = Meeting.Create(_validUserId);

        // Act
        meeting.UpdateAttendees(_validAttendees);

        // Assert
        Assert.Equal(_validAttendees, meeting.Attendees);
    }

    [Fact]
    public void UpdateAttendees_WithNull_SetsNull()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);

        // Act
        meeting.UpdateAttendees("   ");

        // Assert
        Assert.Null(meeting.Attendees);
    }

    [Fact]
    public void UpdateAttendees_TrimsWhitespace()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId);

        // Act
        meeting.UpdateAttendees("  John, Sarah  ");

        // Assert
        Assert.Equal("John, Sarah", meeting.Attendees);
    }

    [Fact]
    public void UpdateAttendees_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);

        // Act
        meeting.UpdateStatus(newStatus);

        // Assert
        Assert.Equal(newStatus, meeting.Status);
    }

    [Fact]
    public void UpdateStatus_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);

        // Act
        meeting.SubmitTranscript("  Transcript content  ");

        // Assert
        Assert.Equal("Transcript content", meeting.TranscriptContent);
    }

    [Fact]
    public void SubmitTranscript_UpdatesUpdatedAt()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);

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
        var meeting = Meeting.Create(_validUserId);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            meeting.SubmitTranscript(invalidTranscript));
    }

    [Fact]
    public void SubmitTranscript_OverwritesExistingTranscript()
    {
        // Arrange
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);
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
        var meeting = Meeting.Create(_validUserId);
        var originalUpdatedAt = meeting.UpdatedAt;

        // Act
        meeting.ClearTranscript();

        // Assert
        Assert.Equal(originalUpdatedAt, meeting.UpdatedAt);
    }

    #endregion
}
