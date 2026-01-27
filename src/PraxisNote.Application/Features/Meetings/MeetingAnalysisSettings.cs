namespace PraxisNote.Application.Features.Meetings;

public class MeetingAnalysisSettings
{
    public const string SectionName = "MeetingAnalysis";

    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "claude-sonnet-4-20250514";
    public int MaxTokens { get; set; } = 4096;
    public int TimeoutSeconds { get; set; } = 120;
}
