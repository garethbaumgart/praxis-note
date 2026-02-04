using NSubstitute;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;

namespace PraxisNote.Application.Tests.Meetings;

public sealed class ExtractMeetingsFromScreenshotTests
{
    private readonly IMeetingAnalyzer _analyzer = Substitute.For<IMeetingAnalyzer>();
    private readonly ExtractMeetingsFromScreenshot _sut;

    public ExtractMeetingsFromScreenshotTests()
    {
        _sut = new ExtractMeetingsFromScreenshot(_analyzer);
    }

    #region ExecuteAsync

    [Fact]
    public async Task ExecuteAsync_WithValidImage_ReturnsExtractedEvents()
    {
        // Arrange
        var events = new List<ExtractedCalendarEvent>
        {
            new("Team Standup", DateTimeOffset.Parse("2025-01-15T09:00:00Z"), DateTimeOffset.Parse("2025-01-15T09:30:00Z"), "Alice, Bob", null),
            new("Sprint Review", DateTimeOffset.Parse("2025-01-15T14:00:00Z"), DateTimeOffset.Parse("2025-01-15T15:00:00Z"), "Team", "Room 3B"),
        };
        _analyzer.ExtractFromScreenshotAsync("base64data", "image/png", Arg.Any<CancellationToken>())
            .Returns(new ScreenshotExtractionResult(events));

        var command = new ExtractMeetingsFromScreenshot.Command(Guid.NewGuid(), "base64data", "image/png");

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("Team Standup", result.Events[0].Title);
        Assert.Equal("Sprint Review", result.Events[1].Title);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoEventsFound_ReturnsEmptyList()
    {
        // Arrange
        _analyzer.ExtractFromScreenshotAsync("base64data", "image/jpeg", Arg.Any<CancellationToken>())
            .Returns(new ScreenshotExtractionResult([]));

        var command = new ExtractMeetingsFromScreenshot.Command(Guid.NewGuid(), "base64data", "image/jpeg");

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Empty(result.Events);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCorrectParametersToAnalyzer()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _analyzer.ExtractFromScreenshotAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ScreenshotExtractionResult([]));

        var command = new ExtractMeetingsFromScreenshot.Command(userId, "imagedata", "image/webp");

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        await _analyzer.Received(1).ExtractFromScreenshotAsync("imagedata", "image/webp", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _analyzer.ExtractFromScreenshotAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var token = callInfo.ArgAt<CancellationToken>(2);
                token.ThrowIfCancellationRequested();
                return new ScreenshotExtractionResult([]);
            });

        var command = new ExtractMeetingsFromScreenshot.Command(Guid.NewGuid(), "data", "image/png");
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.ExecuteAsync(command, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_PreservesEventDetails()
    {
        // Arrange
        var start = DateTimeOffset.Parse("2025-03-20T10:00:00+02:00");
        var end = DateTimeOffset.Parse("2025-03-20T11:30:00+02:00");
        var events = new List<ExtractedCalendarEvent>
        {
            new("1:1 with Manager", start, end, "Jane Doe", "Conference Room A"),
        };
        _analyzer.ExtractFromScreenshotAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ScreenshotExtractionResult(events));

        var command = new ExtractMeetingsFromScreenshot.Command(Guid.NewGuid(), "data", "image/png");

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        var evt = Assert.Single(result.Events);
        Assert.Equal("1:1 with Manager", evt.Title);
        Assert.Equal(start, evt.StartTime);
        Assert.Equal(end, evt.EndTime);
        Assert.Equal("Jane Doe", evt.Attendees);
        Assert.Equal("Conference Room A", evt.Location);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOptionalFields_ReturnsEventWithNulls()
    {
        // Arrange
        var events = new List<ExtractedCalendarEvent>
        {
            new("Quick Sync", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15), null, null),
        };
        _analyzer.ExtractFromScreenshotAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ScreenshotExtractionResult(events));

        var command = new ExtractMeetingsFromScreenshot.Command(Guid.NewGuid(), "data", "image/png");

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        var evt = Assert.Single(result.Events);
        Assert.Null(evt.Attendees);
        Assert.Null(evt.Location);
    }

    #endregion
}
