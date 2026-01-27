namespace PraxisNote.Application.Features.Meetings.Services;

public interface IMeetingAnalyzer
{
    Task<MeetingAnalysisResult> AnalyzeAsync(string transcript, CancellationToken cancellationToken = default);
}

public record MeetingAnalysisResult(
    string Summary,
    List<string> KeyPoints,
    List<string> Decisions);
