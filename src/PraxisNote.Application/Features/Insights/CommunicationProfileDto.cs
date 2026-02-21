namespace PraxisNote.Application.Features.Insights;

public record CommunicationProfileDto(
    string PrimaryArchetype,
    string PrimaryDescription,
    string? SecondaryArchetype,
    double StyleConsistency,
    int MeetingCount,
    int MinimumMeetings,
    bool HasEnoughData,
    List<ArchetypeScoreDto> Scores,
    List<ContextShiftDto> ContextShifts,
    List<string> Strengths,
    List<string> GrowthAreas,
    DimensionScoresDto DimensionScores,
    DimensionScoresDto IdealProfile,
    List<ArchetypeTimelinePointDto> ArchetypeTimeline);

public record ArchetypeScoreDto(string Name, double Score);

public record ContextShiftDto(
    string Context,
    string Icon,
    string Archetype,
    string Description);

public record DimensionScoresDto(
    double TalkTime,
    double QuestionRatio,
    double Sentiment,
    double Interruptions,
    double Engagement,
    double Clarity);

public record ArchetypeTimelinePointDto(
    DateOnly WeekStartDate,
    string Archetype,
    double Score);
