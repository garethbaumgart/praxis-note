namespace PraxisNote.Application.Features.Meetings.Services;

public interface IMeetingAnalyzer
{
    Task<MeetingAnalysisResult> AnalyzeAsync(string transcript, CancellationToken cancellationToken = default);
    Task<ScreenshotExtractionResult> ExtractFromScreenshotAsync(string base64Image, string mediaType, string? timeZone = null, CancellationToken cancellationToken = default);
    Task<TranscriptImportResult> ParseTranscriptForImportAsync(string transcript, string? timeZone = null, CancellationToken cancellationToken = default);
}

public record ScreenshotExtractionResult(List<ExtractedCalendarEvent> Events);

public record ExtractedCalendarEvent(
    string Title,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Attendees,
    string? Location);

public record TranscriptImportResult(
    string? Title,
    DateTimeOffset? MeetingDate,
    string? Attendees,
    string Summary,
    List<string> KeyPoints,
    List<string> Decisions,
    List<ExtractedActionItem> ActionItems,
    List<string> SuggestedTags,
    bool IsComplete,
    string? Warning,
    bool IsAdhoc = false);

public record MeetingAnalysisResult(
    string Summary,
    List<string> KeyPoints,
    List<string> Decisions,
    List<string> ExtractedAttendees = default!,
    List<ExtractedActionItem> ExtractedActionItems = default!,
    string? SuggestedTitle = null,
    List<string> SuggestedTags = default!)
{
    public List<string> ExtractedAttendees { get; init; } = ExtractedAttendees ?? [];
    public List<ExtractedActionItem> ExtractedActionItems { get; init; } = ExtractedActionItems ?? [];
    public List<string> SuggestedTags { get; init; } = SuggestedTags ?? [];
}

/// <summary>
/// Action item extracted from meeting transcript by AI.
/// </summary>
public record ExtractedActionItem(string Description, string? Assignee);
