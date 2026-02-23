using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;

namespace PraxisNote.Application.Tests.Meetings;

public class ParseTranscriptForImportTests
{
    private readonly IMeetingAnalyzer _meetingAnalyzer = Substitute.For<IMeetingAnalyzer>();
    private readonly ITranscriptExtractor _transcriptExtractor = Substitute.For<ITranscriptExtractor>();
    private readonly ParseTranscriptForImport _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public ParseTranscriptForImportTests()
    {
        _sut = new ParseTranscriptForImport(_meetingAnalyzer, _transcriptExtractor);
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

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, transcript, null, null, null);

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
        Assert.Equal(2, result.SuggestedTags.Count);
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

        _meetingAnalyzer.ParseTranscriptForImportAsync(extractedText, Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, null, stream, contentType, "meeting.docx");

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
        var command = new ParseTranscriptForImport.Command(_userId, null, null, null, null);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmptyText_ThrowsArgumentException()
    {
        var command = new ParseTranscriptForImport.Command(_userId, "   ", null, null, null);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnsupportedFileType_ThrowsInvalidOperationException()
    {
        var stream = new MemoryStream();
        var command = new ParseTranscriptForImport.Command(_userId, null, stream, "application/pdf", "meeting.pdf");

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

        _meetingAnalyzer.ParseTranscriptForImportAsync(extractedText, Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, null, stream, "text/plain", "meeting.txt");

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
        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Claude returned an empty response"));

        var command = new ParseTranscriptForImport.Command(_userId, transcript, null, null, null);

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

        _meetingAnalyzer.ParseTranscriptForImportAsync(transcript, Arg.Any<CancellationToken>())
            .Returns(parseResult);

        var command = new ParseTranscriptForImport.Command(_userId, transcript, stream, "text/plain", "file.txt");

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert - text was used, not the file
        Assert.Equal(transcript, result.Transcript);
        _transcriptExtractor.DidNotReceive().ExtractTextFromPlainText(Arg.Any<Stream>());
    }

    #endregion
}
