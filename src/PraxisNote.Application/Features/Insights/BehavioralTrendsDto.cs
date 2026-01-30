namespace PraxisNote.Application.Features.Insights;

public record BehavioralTrendsDto(
    string ParticipantName,
    List<string> AvailableParticipants,
    int MeetingCount,
    TrendSummaryDto Summary,
    TrendSeriesDto TalkTimeTrend,
    TrendSeriesDto QuestionRatioTrend,
    TrendSeriesDto InterruptionTrend,
    TrendSeriesDto SentimentTrend,
    RedFlagTrendDto RedFlagTrend,
    TrendSeriesDto EngagementTrend);

public record TrendSummaryDto(
    double AverageTalkTimePercent,
    double TalkTimeChange,
    double AverageQuestionRatio,
    double QuestionRatioChange,
    double AverageInterruptionCount,
    double InterruptionChange,
    double AverageSentimentScore,
    double SentimentChange,
    int TotalRedFlags,
    double RedFlagChange,
    string DominantEngagementLevel);

public record TrendDataPoint(DateTimeOffset Date, double Value, string? Label = null);

public record TrendSeriesDto(List<TrendDataPoint> DataPoints);

public record RedFlagTrendDto(
    List<TrendDataPoint> TotalByMeeting,
    Dictionary<string, List<TrendDataPoint>> ByType);
