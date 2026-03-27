using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.UserAiKeys.Services;

namespace PraxisNote.Application.Tests.Meetings;

public class ParseTranscriptForImportTests
{
    private readonly IMeetingAnalyzer _meetingAnalyzer = Substitute.For<IMeetingAnalyzer>();
    private readonly IResolvedAiServices _aiServices = Substitute.For<IResolvedAiServices>();
    private readonly ITranscriptExtractor _transcriptExtractor = Substitute.For<ITranscriptExtractor>();
    private readonly ParseTranscriptForImport _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public ParseTranscriptForImportTests()
    {
        _aiServices.GetMeetingAnalyzerAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_meetingAnalyzer);
        _sut = new ParseTranscriptForImport(_aiServices, _transcriptExtractor);
    }

    #region Text Input

    [Fact]
    public async Task ExecuteAsync_WithPlainText_ExtractsAndParsesViaMeetingAnalyzer()
    {
        // Arrange
        var transcript = "Meeting transcript about Q3 budget review...";
        var parseResult = new TranscriptImportResult(
            Title: "Q3 Budget Review",
            MeetingDate: new DateTimeOffset(2025, 6, 15, 9, 0, 0, TimeSpan.Zero),
            Attendees: "Alice, Bob",
            Summary: "Discussed Q3 budget allocations",
            KeyPoints: ["Budget increased by 10%", "New hiring plan approved"],
            Decisions: ["Approve Q3 budget"],
            ActionItems: [new ExtractedActionItem("Send updated proposal", "Alice")],
            SuggestedTags: ["budget", "planning"],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, null, null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal("Q3 Budget Review", result.Title);
        Assert.NotNull(result.MeetingDate);
        Assert.Equal("Alice, Bob", result.Attendees);
        Assert.Equal("Discussed Q3 budget allocations", result.Summary);
        Assert.Equal(2, result.KeyPoints!.Count);
        Assert.Single(result.Decisions!);
        Assert.Single(result.ActionItems!);
        Assert.Equal("Send updated proposal", result.ActionItems![0].Description);
        Assert.Equal("Alice", result.ActionItems![0].Assignee);
        Assert.Equal(transcript, result.Transcript);
        Assert.True(result.IsComplete);
        Assert.Null(result.Warning);
        Assert.Equal(4, result.SuggestedTags.Count);
        Assert.Equal("alice", result.SuggestedTags[0]);
        Assert.Equal("bob", result.SuggestedTags[1]);
        Assert.Equal("budget", result.SuggestedTags[2]);
        Assert.Equal("planning", result.SuggestedTags[3]);
    }

    #endregion

    #region File Input

    [Fact]
    public async Task ExecuteAsync_WithDocxFile_ExtractsTextThenParsesViaMeetingAnalyzer()
    {
        // Arrange
        var extractedText = "Extracted docx content about project standup...";
        var stream = new MemoryStream();
        var contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        _transcriptExtractor.ExtractTextFromDocxAsync(stream, Arg.Any<CancellationToken>())
            .Returns(extractedText);

        var parseResult = new TranscriptImportResult(
            Title: "Project Standup",
            MeetingDate: null,
            Attendees: null,
            Summary: "Daily standup discussion",
            KeyPoints: ["Sprint progress reviewed"],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["standup"],
            IsComplete: false,
            Warning: "Could not extract meeting date or attendees");

        _meetingAnalyzer.ParseTranscriptForImportAsync(extractedText, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, null, null, null, stream, contentType, "meeting.docx");

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal("Project Standup", result.Title);
        Assert.Null(result.MeetingDate);
        Assert.Equal(extractedText, result.Transcript);
        Assert.False(result.IsComplete);
        Assert.NotNull(result.Warning);
        await _transcriptExtractor.Received(1).ExtractTextFromDocxAsync(stream, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation

    [Fact]
    public async Task ExecuteAsync_WhenNoTextOrFile_ThrowsArgumentException()
    {
        var command = new ParseTranscriptForImport.Command(_userId, null, null, null, null, null, null);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmptyText_ThrowsArgumentException()
    {
        var command = new ParseTranscriptForImport.Command(_userId, null, null, "   ", null, null, null);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnsupportedFileType_ThrowsInvalidOperationException()
    {
        var stream = new MemoryStream();
        var command = new ParseTranscriptForImport.Command(_userId, null, null, null, stream, "application/pdf", "meeting.pdf");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WithPlainTextFile_UsesPlainTextExtractor()
    {
        // Arrange
        var extractedText = "Plain text content...";
        var stream = new MemoryStream();

        _transcriptExtractor.ExtractTextFromPlainText(stream).Returns(extractedText);

        var parseResult = new TranscriptImportResult(
            Title: "Meeting",
            MeetingDate: null,
            Attendees: null,
            Summary: "A meeting",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: [],
            IsComplete: false,
            Warning: "Missing date and attendees");

        _meetingAnalyzer.ParseTranscriptForImportAsync(extractedText, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, null, null, null, stream, "text/plain", "meeting.txt");

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(extractedText, result.Transcript);
        _transcriptExtractor.Received(1).ExtractTextFromPlainText(stream);
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task ExecuteAsync_WhenAIFails_PropagatesException()
    {
        var transcript = "Some transcript text...";
        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Claude returned an empty response"));

        var command = new ParseTranscriptForImport.Command(_userId, null, null, transcript, null, null, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTextAndFileProvided_UsesTextInput()
    {
        // Arrange - both text and file provided, text should be used
        var transcript = "Direct text input...";
        var stream = new MemoryStream();

        var parseResult = new TranscriptImportResult(
            Title: "Test",
            MeetingDate: null,
            Attendees: null,
            Summary: "Test summary",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: [],
            IsComplete: false,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, null, null, transcript, stream, "text/plain", "file.txt");

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert - text was used, not the file
        Assert.Equal(transcript, result.Transcript);
        _transcriptExtractor.DidNotReceive().ExtractTextFromPlainText(Arg.Any<Stream>());
    }

    #endregion

    #region Attendee Person Tags

    [Fact]
    public async Task ExecuteAsync_WithMultipleAttendees_PrependsPersonTagsExcludingUser()
    {
        // Arrange — 3 attendees including the user
        var transcript = "Group meeting transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "Team Planning",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice Smith, Bob Jones, Charlie Brown",
            Summary: "Planning session",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["planning", "sprint"],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice Smith", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — 2 person tags prepended in firstname-lastname format, user excluded
        Assert.Equal(4, result.SuggestedTags.Count);
        Assert.Equal("bob-jones", result.SuggestedTags[0]);
        Assert.Equal("charlie-brown", result.SuggestedTags[1]);
        Assert.Equal("planning", result.SuggestedTags[2]);
        Assert.Equal("sprint", result.SuggestedTags[3]);
    }

    [Fact]
    public async Task ExecuteAsync_WithTwoAttendees_PrependsOtherPersonTag()
    {
        // Arrange — 1:1 meeting, other person's full-name tag prepended
        var transcript = "1:1 meeting transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "1:1 with Bob",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice Smith, Bob Jones",
            Summary: "1:1 discussion",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["career", "feedback"],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice Smith", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — "bob-jones" prepended to AI-suggested tags
        Assert.Equal(3, result.SuggestedTags.Count);
        Assert.Equal("bob-jones", result.SuggestedTags[0]);
        Assert.Equal("career", result.SuggestedTags[1]);
        Assert.Equal("feedback", result.SuggestedTags[2]);
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleAttendee_MatchingUser_NoPersonTags()
    {
        // Arrange — only attendee is the user
        var transcript = "Solo transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "Notes",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice Smith",
            Summary: "Solo notes",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["notes"],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice Smith", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — no person tags, only AI suggestions
        Assert.Single(result.SuggestedTags);
        Assert.Equal("notes", result.SuggestedTags[0]);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullAttendees_NoPersonTags()
    {
        // Arrange
        var transcript = "Transcript without attendees...";
        var parseResult = new TranscriptImportResult(
            Title: "Meeting",
            MeetingDate: null,
            Attendees: null,
            Summary: "Discussion",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["general"],
            IsComplete: false,
            Warning: "Missing attendees");

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — null attendees, no crash, no person tags
        Assert.Single(result.SuggestedTags);
        Assert.Equal("general", result.SuggestedTags[0]);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyAttendees_NoPersonTags()
    {
        // Arrange
        var transcript = "Transcript with empty attendees...";
        var parseResult = new TranscriptImportResult(
            Title: "Meeting",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "   ",
            Summary: "Discussion",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["general"],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — whitespace attendees, no person tags
        Assert.Single(result.SuggestedTags);
        Assert.Equal("general", result.SuggestedTags[0]);
    }

    [Fact]
    public async Task ExecuteAsync_PersonTagAlreadyInAISuggestions_NoDuplicate()
    {
        // Arrange — AI already suggested "Bob-Jones" (mixed case) while person tag generates "bob-jones"
        var transcript = "1:1 transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "1:1 with Bob",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice Smith, Bob Jones",
            Summary: "1:1",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["Bob-Jones", "career"],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice Smith", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — person tag "bob-jones" added first, AI tag "Bob-Jones" skipped (case-insensitive dedup)
        Assert.Equal(2, result.SuggestedTags.Count);
        Assert.Equal("bob-jones", result.SuggestedTags[0]);
        Assert.Equal("career", result.SuggestedTags[1]);
    }

    [Fact]
    public async Task ExecuteAsync_PersonTagsOrderedBeforeAITags()
    {
        // Arrange — verify person tags come first, AI topic tags after
        var transcript = "Group meeting...";
        var parseResult = new TranscriptImportResult(
            Title: "Team Sync",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice Smith, Bob Jones, Charlie Brown",
            Summary: "Team sync",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["engineering", "planning"],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice Smith", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — person tags first, then AI tags
        Assert.Equal(4, result.SuggestedTags.Count);
        Assert.Equal("bob-jones", result.SuggestedTags[0]);
        Assert.Equal("charlie-brown", result.SuggestedTags[1]);
        Assert.Equal("engineering", result.SuggestedTags[2]);
        Assert.Equal("planning", result.SuggestedTags[3]);
    }

    [Fact]
    public async Task ExecuteAsync_AttendeeWithSingleName_GeneratesTag()
    {
        // Arrange — attendee "Bob" has no surname
        var transcript = "Meeting transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "Chat with Bob",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice Smith, Bob",
            Summary: "Quick chat",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["chat"],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice Smith", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — single-name attendee generates tag "bob"
        Assert.Equal(2, result.SuggestedTags.Count);
        Assert.Equal("bob", result.SuggestedTags[0]);
        Assert.Equal("chat", result.SuggestedTags[1]);
    }

    [Fact]
    public async Task ExecuteAsync_UserMatchByFirstName_ExcludesUser()
    {
        // Arrange — user "Alice Smith", attendee "Alice" (first-name fallback match)
        var transcript = "Meeting transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "Chat",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice, Bob Jones",
            Summary: "Discussion",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["chat"],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice Smith", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — "Alice" excluded via first-name match, only "bob-jones" added
        Assert.Equal(2, result.SuggestedTags.Count);
        Assert.Equal("bob-jones", result.SuggestedTags[0]);
        Assert.Equal("chat", result.SuggestedTags[1]);
    }

    [Fact]
    public async Task ExecuteAsync_WithAttendees_AndNullUserName_IncludesAllPersonTags()
    {
        // Arrange — userName is null, all attendees should be included as person tags
        var transcript = "Meeting transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "Chat",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice, Bob Jones",
            Summary: "Discussion",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["chat"],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, null, null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — both attendees are included as person tags when userName is unknown
        Assert.Equal(3, result.SuggestedTags.Count);
        Assert.Equal("alice", result.SuggestedTags[0]);
        Assert.Equal("bob-jones", result.SuggestedTags[1]);
        Assert.Equal("chat", result.SuggestedTags[2]);
    }

    [Fact]
    public async Task ExecuteAsync_DifferentFullNameSameFirstName_NotExcluded()
    {
        // Arrange — user "Alice Smith", attendee "Alice Johnson" should NOT be excluded
        var transcript = "Meeting transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "Team Sync",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice Smith, Alice Johnson, Bob Jones",
            Summary: "Team sync",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["sync"],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice Smith", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — "Alice Smith" excluded (exact match), "Alice Johnson" kept (different full name)
        Assert.Equal(3, result.SuggestedTags.Count);
        Assert.Equal("alice-johnson", result.SuggestedTags[0]);
        Assert.Equal("bob-jones", result.SuggestedTags[1]);
        Assert.Equal("sync", result.SuggestedTags[2]);
    }

    #endregion

    #region Ad-Hoc Detection

    [Fact]
    public async Task ExecuteAsync_WhenIsAdhoc_AddsAdhocCallTag()
    {
        // Arrange
        var transcript = "Ad-hoc meeting transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "Quick Sync",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice Smith, Bob Jones",
            Summary: "Impromptu discussion",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["engineering"],
            IsComplete: true,
            Warning: null,
            IsAdhoc: true);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice Smith", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — person tag, AI tag, adhoc-call last; no duplicates
        Assert.Equal(3, result.SuggestedTags.Count);
        Assert.Equal("bob-jones", result.SuggestedTags[0]);    // person tag
        Assert.Equal("engineering", result.SuggestedTags[1]);  // AI tag
        Assert.Equal("adhoc-call", result.SuggestedTags[2]);   // adhoc-call last
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotAdhoc_DoesNotAddAdhocCallTag()
    {
        // Arrange
        var transcript = "Scheduled meeting transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "Sprint Planning",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice Smith, Bob Jones",
            Summary: "Sprint planning session",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["planning", "sprint"],
            IsComplete: true,
            Warning: null,
            IsAdhoc: false);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice Smith", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — no adhoc-call tag
        Assert.DoesNotContain("adhoc-call", result.SuggestedTags);
    }

    [Theory]
    [InlineData("adhoc-call")]
    [InlineData("AdHoc-Call")]
    [InlineData("ADHOC-CALL")]
    public async Task ExecuteAsync_WhenIsAdhoc_AndAISuggestsVariantCasing_NoDuplicate(string existingAdhocTag)
    {
        // Arrange — AI already suggests a casing variant of "adhoc-call" and IsAdhoc is true
        var transcript = "Ad-hoc meeting transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "Quick Chat",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: null,
            Summary: "Ad-hoc discussion",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["engineering", existingAdhocTag],
            IsComplete: true,
            Warning: null,
            IsAdhoc: true);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, null, null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — exactly one adhoc-call-equivalent tag regardless of input casing
        Assert.Equal(1, result.SuggestedTags.Count(t => string.Equals(t, "adhoc-call", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task ExecuteAsync_WhenIsAdhoc_AdhocCallTagAppearsAfterOtherTags()
    {
        // Arrange — verify ordering: person tags → AI tags → adhoc-call
        var transcript = "Ad-hoc group meeting...";
        var parseResult = new TranscriptImportResult(
            Title: "Impromptu Sync",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: "Alice Smith, Bob Jones",
            Summary: "Quick sync",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: ["engineering", "sync"],
            IsComplete: true,
            Warning: null,
            IsAdhoc: true);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, "Alice Smith", null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — person tags first, then AI tags, then adhoc-call last
        Assert.Equal(4, result.SuggestedTags.Count);
        Assert.Equal("bob-jones", result.SuggestedTags[0]);      // person tag
        Assert.Equal("engineering", result.SuggestedTags[1]);     // AI tag
        Assert.Equal("sync", result.SuggestedTags[2]);            // AI tag
        Assert.Equal("adhoc-call", result.SuggestedTags[3]);      // adhoc-call last
    }

    #endregion

    #region Meeting Date Format

    [Fact]
    public async Task ExecuteAsync_MeetingDate_FormatsAsJsSafeIso8601WithOffset()
    {
        // Arrange — known date with explicit offset
        var transcript = "Meeting transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "Morning Standup",
            MeetingDate: new DateTimeOffset(2026, 2, 25, 5, 59, 0, TimeSpan.FromHours(11)),
            Attendees: null,
            Summary: "Standup",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: [],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, null, null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert — JS-safe ISO 8601: no fractional seconds, explicit offset
        Assert.Equal("2026-02-25T05:59:00+11:00", result.MeetingDate);
    }

    [Fact]
    public async Task ExecuteAsync_NullMeetingDate_ReturnsNull()
    {
        // Arrange
        var transcript = "Transcript without date...";
        var parseResult = new TranscriptImportResult(
            Title: "Undated Meeting",
            MeetingDate: null,
            Attendees: null,
            Summary: "Discussion",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: [],
            IsComplete: false,
            Warning: "No date found");

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, null, null, transcript, null, null, null);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Null(result.MeetingDate);
    }

    #endregion

    #region Timezone

    [Fact]
    public async Task ExecuteAsync_WithTimeZone_PassesTimeZoneToAnalyzer()
    {
        // Arrange
        var transcript = "Meeting transcript...";
        var timeZone = "Australia/Sydney";
        var parseResult = new TranscriptImportResult(
            Title: "Meeting",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: null,
            Summary: "Discussion",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: [],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, timeZone, Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, null, timeZone, transcript, null, null, null);

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        await _meetingAnalyzer.Received(1).ParseTranscriptForImportAsync(transcript, timeZone, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNullTimeZone_PassesNullToAnalyzer()
    {
        // Arrange
        var transcript = "Meeting transcript...";
        var parseResult = new TranscriptImportResult(
            Title: "Meeting",
            MeetingDate: DateTimeOffset.UtcNow,
            Attendees: null,
            Summary: "Discussion",
            KeyPoints: [],
            Decisions: [],
            ActionItems: [],
            SuggestedTags: [],
            IsComplete: true,
            Warning: null);

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, null, Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, null, null, transcript, null, null, null);

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        await _meetingAnalyzer.Received(1).ParseTranscriptForImportAsync(transcript, null, Arg.Any<CancellationToken>());
    }

    #endregion
}
