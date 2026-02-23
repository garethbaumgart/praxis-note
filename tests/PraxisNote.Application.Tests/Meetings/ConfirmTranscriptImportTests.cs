using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Tests.Meetings;

public class ConfirmTranscriptImportTests
{
    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly ITagRepository _tagRepo = Substitute.For<ITagRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ConfirmTranscriptImport _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public ConfirmTranscriptImportTests()
    {
        _sut = new ConfirmTranscriptImport(_meetingRepo, _tagRepo, _unitOfWork);
    }

    #region Basic Import

    [Fact]
    public async Task ExecuteAsync_WithValidMeetings_CreatesAllMeetingsWithCorrectState()
    {
        // Arrange
        SetupEmptyTags();
        var meetings = new List<ConfirmTranscriptImport.ImportItem>
        {
            CreateImportItem("Budget Review", "Meeting about budget"),
            CreateImportItem("Sprint Retro", "Sprint retrospective"),
        };

        var command = new ConfirmTranscriptImport.Command(_userId, _profileId, meetings);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(2, result.ImportedCount);
        await _meetingRepo.Received(2).AddAsync(Arg.Any<Meeting>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_AppliesTranscriptAndAnalysisResults()
    {
        // Arrange
        SetupEmptyTags();
        var meetings = new List<ConfirmTranscriptImport.ImportItem>
        {
            new(
                Title: "Q3 Review",
                MeetingDate: new DateTimeOffset(2025, 6, 15, 9, 0, 0, TimeSpan.Zero),
                Attendees: "Alice, Bob",
                Transcript: "The meeting transcript content...",
                Summary: "Discussed Q3 goals",
                KeyPoints: "[\"Point 1\",\"Point 2\"]",
                Decisions: "[\"Decision 1\"]",
                ActionItems:
                [
                    new ConfirmTranscriptImport.ActionItemInput("Send report", "Alice"),
                    new ConfirmTranscriptImport.ActionItemInput("Update docs", null)
                ],
                SuggestedTags: [])
        };

        var command = new ConfirmTranscriptImport.Command(_userId, _profileId, meetings);

        Meeting? capturedMeeting = null;
        await _meetingRepo.AddAsync(Arg.Do<Meeting>(m => capturedMeeting = m), Arg.Any<CancellationToken>());

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.NotNull(capturedMeeting);
        Assert.Equal("Q3 Review", capturedMeeting!.Title);
        Assert.Equal("The meeting transcript content...", capturedMeeting.TranscriptContent);
        Assert.Equal("Discussed Q3 goals", capturedMeeting.Summary);
        Assert.Equal(MeetingStatus.Ready, capturedMeeting.Status);
        Assert.Equal(2, capturedMeeting.ActionItems.Count);
        Assert.Equal(2, result.TotalActionItems);
    }

    #endregion

    #region Tag Matching

    [Fact]
    public async Task ExecuteAsync_AddsMatchingTagsFromSuggested()
    {
        // Arrange
        var budgetTagId = Guid.NewGuid();
        var budgetTag = Tag.Create(_userId, _profileId, "budget");

        _tagRepo.GetByNamesAsync(_userId, _profileId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tag> { budgetTag });

        var meetings = new List<ConfirmTranscriptImport.ImportItem>
        {
            new(
                Title: "Budget Meeting",
                MeetingDate: DateTimeOffset.UtcNow,
                Attendees: null,
                Transcript: "Transcript...",
                Summary: "Budget discussion",
                KeyPoints: null,
                Decisions: null,
                ActionItems: [],
                SuggestedTags: ["budget", "planning"])
        };

        var command = new ConfirmTranscriptImport.Command(_userId, _profileId, meetings);

        Meeting? capturedMeeting = null;
        await _meetingRepo.AddAsync(Arg.Do<Meeting>(m => capturedMeeting = m), Arg.Any<CancellationToken>());

        // Act
        await _sut.ExecuteAsync(command);

        // Assert - budget tag should be added, planning tag should be ignored (not found)
        Assert.NotNull(capturedMeeting);
        Assert.Contains(budgetTag.Id, capturedMeeting!.TagIds);
        Assert.Single(capturedMeeting.TagIds);
    }

    [Fact]
    public async Task ExecuteAsync_IgnoresSuggestedTagsNotInUserTagList()
    {
        // Arrange - no matching tags exist
        _tagRepo.GetByNamesAsync(_userId, _profileId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tag>());

        var meetings = new List<ConfirmTranscriptImport.ImportItem>
        {
            new(
                Title: "Meeting",
                MeetingDate: DateTimeOffset.UtcNow,
                Attendees: null,
                Transcript: "Transcript...",
                Summary: "Summary",
                KeyPoints: null,
                Decisions: null,
                ActionItems: [],
                SuggestedTags: ["nonexistent", "also-missing"])
        };

        var command = new ConfirmTranscriptImport.Command(_userId, _profileId, meetings);

        Meeting? capturedMeeting = null;
        await _meetingRepo.AddAsync(Arg.Do<Meeting>(m => capturedMeeting = m), Arg.Any<CancellationToken>());

        // Act
        await _sut.ExecuteAsync(command);

        // Assert - no tags applied
        Assert.NotNull(capturedMeeting);
        Assert.Empty(capturedMeeting!.TagIds);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ExecuteAsync_WithEmptyList_ReturnsZeroImported()
    {
        var command = new ConfirmTranscriptImport.Command(_userId, _profileId, []);

        var result = await _sut.ExecuteAsync(command);

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.TotalActionItems);
        await _meetingRepo.DidNotReceive().AddAsync(Arg.Any<Meeting>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SetsStatusToReady()
    {
        // Arrange
        SetupEmptyTags();
        var meetings = new List<ConfirmTranscriptImport.ImportItem>
        {
            CreateImportItem("Test Meeting", "Test summary")
        };

        var command = new ConfirmTranscriptImport.Command(_userId, _profileId, meetings);

        Meeting? capturedMeeting = null;
        await _meetingRepo.AddAsync(Arg.Do<Meeting>(m => capturedMeeting = m), Arg.Any<CancellationToken>());

        // Act
        await _sut.ExecuteAsync(command);

        // Assert - CompleteAnalysis sets status to Ready
        Assert.NotNull(capturedMeeting);
        Assert.Equal(MeetingStatus.Ready, capturedMeeting!.Status);
    }

    [Fact]
    public async Task ExecuteAsync_FiltersEmptyActionItemDescriptions()
    {
        // Arrange
        SetupEmptyTags();
        var meetings = new List<ConfirmTranscriptImport.ImportItem>
        {
            new(
                Title: "Meeting",
                MeetingDate: DateTimeOffset.UtcNow,
                Attendees: null,
                Transcript: "Transcript...",
                Summary: "Summary",
                KeyPoints: null,
                Decisions: null,
                ActionItems:
                [
                    new ConfirmTranscriptImport.ActionItemInput("Valid task", null),
                    new ConfirmTranscriptImport.ActionItemInput("", null),
                    new ConfirmTranscriptImport.ActionItemInput("  ", null),
                    new ConfirmTranscriptImport.ActionItemInput("Another valid task", "Bob")
                ],
                SuggestedTags: [])
        };

        var command = new ConfirmTranscriptImport.Command(_userId, _profileId, meetings);

        Meeting? capturedMeeting = null;
        await _meetingRepo.AddAsync(Arg.Do<Meeting>(m => capturedMeeting = m), Arg.Any<CancellationToken>());

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert - only 2 valid action items
        Assert.Equal(2, result.TotalActionItems);
        Assert.NotNull(capturedMeeting);
        Assert.Equal(2, capturedMeeting!.ActionItems.Count);
    }

    [Fact]
    public async Task ExecuteAsync_UsesDefaultSummaryWhenNull()
    {
        // Arrange
        SetupEmptyTags();
        var meetings = new List<ConfirmTranscriptImport.ImportItem>
        {
            new(
                Title: "Meeting",
                MeetingDate: DateTimeOffset.UtcNow,
                Attendees: null,
                Transcript: "Transcript...",
                Summary: null,
                KeyPoints: null,
                Decisions: null,
                ActionItems: [],
                SuggestedTags: [])
        };

        var command = new ConfirmTranscriptImport.Command(_userId, _profileId, meetings);

        Meeting? capturedMeeting = null;
        await _meetingRepo.AddAsync(Arg.Do<Meeting>(m => capturedMeeting = m), Arg.Any<CancellationToken>());

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        Assert.NotNull(capturedMeeting);
        Assert.Equal("Imported meeting", capturedMeeting!.Summary);
    }

    #endregion

    #region Helpers

    private void SetupEmptyTags()
    {
        _tagRepo.GetByNamesAsync(_userId, _profileId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tag>());
    }

    private static ConfirmTranscriptImport.ImportItem CreateImportItem(string title, string summary)
    {
        return new ConfirmTranscriptImport.ImportItem(
            Title: title,
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice, Bob",
            Transcript: "Sample transcript text...",
            Summary: summary,
            KeyPoints: "[\"Point 1\"]",
            Decisions: null,
            ActionItems: [new ConfirmTranscriptImport.ActionItemInput("Follow up", "Alice")],
            SuggestedTags: []);
    }

    #endregion
}
