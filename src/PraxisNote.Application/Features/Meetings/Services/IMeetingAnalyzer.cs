namespace PraxisNote.Application.Features.Meetings.Services;

public interface IMeetingAnalyzer
{
    Task<MeetingAnalysisResult> AnalyzeAsync(string transcript, CancellationToken cancellationToken = default);
    Task<ScreenshotExtractionResult> ExtractFromScreenshotAsync(string base64Image, string mediaType, CancellationToken cancellationToken = default);
}

public record ScreenshotExtractionResult(List<ExtractedCalendarEvent> Events);

public record ExtractedCalendarEvent(
    string Title,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Attendees,
    string? Location);

public record MeetingAnalysisResult(
    string Summary,
    List<string> KeyPoints,
    List<string> Decisions,
    BehavioralAnalysisData? BehavioralAnalysis = null,
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

#region Behavioral Analysis Types

public record BehavioralAnalysisData(
    SpeakingDynamics SpeakingDynamics,
    SentimentTone SentimentTone,
    CommunicationPatterns CommunicationPatterns,
    List<RedFlag> RedFlags);

public record SpeakingDynamics(
    List<ParticipantTalkTime> TalkTimeByParticipant,
    List<InterruptionPattern> InterruptionPatterns,
    Dictionary<string, double> QuestionVsStatementRatio);

public record ParticipantTalkTime(string Participant, double Percentage, string Duration);

public record InterruptionPattern(string Interrupter, string Interrupted, int Count);

public record SentimentTone(
    List<ParticipantSentiment> ParticipantSentiments,
    List<ToneShift> ToneShifts,
    List<string> EmotionalIndicators);

public record ParticipantSentiment(string Participant, string Sentiment, double Score);

public record ToneShift(string Timestamp, string Description, string From, string To);

public record CommunicationPatterns(
    double OverallClarity,
    List<FollowUpPattern> FollowUpPatterns,
    List<ParticipantEngagement> EngagementLevels);

public record FollowUpPattern(string Topic, bool WasFollowedUp, string? AssignedTo);

public record ParticipantEngagement(string Participant, string Level, List<string> Indicators);

public record RedFlag(
    string Type,        // "evasive", "hedging", "defensive", "inconsistent"
    string Participant,
    string Description,
    string Context,
    string Severity);   // "low", "medium", "high"

#endregion
