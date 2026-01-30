namespace PraxisNote.Application.Features.Insights;

public record InsightsSummaryDto(
    int MeetingCount,
    string ParticipantName,
    InsightsHeadlineMetric Headline,
    InsightsSecondaryMetric QuestionRatio,
    InsightsSecondaryMetric RedFlags,
    string? NudgeText,
    List<double> SparklineValues);

public record InsightsHeadlineMetric(
    string Label,
    double Value,
    double Change,
    string Unit);

public record InsightsSecondaryMetric(
    string Label,
    double Value,
    double Change);
