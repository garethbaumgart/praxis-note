using System.Text.Json;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Insights;

public sealed class GetBehavioralTrends(IMeetingRepository meetingRepository)
{
    public static readonly string[] ValidRanges = ["7d", "30d", "90d", "all"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public record Query(Guid UserId, string Range, string? ParticipantName = null);

    public async Task<BehavioralTrendsDto> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        if (!ValidRanges.Contains(query.Range))
            throw new ArgumentException($"Invalid range: {query.Range}. Must be one of: {string.Join(", ", ValidRanges)}");

        var allMeetings = await meetingRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        var cutoff = GetCutoffDate(query.Range);

        var meetings = allMeetings
            .Where(m => (m.Status == MeetingStatus.Ready || m.Status == MeetingStatus.Reviewed)
                        && m.BehavioralAnalysis is not null
                        && (m.MeetingDate ?? m.CreatedAt) >= cutoff)
            .OrderBy(m => m.MeetingDate ?? m.CreatedAt)
            .ToList();

        if (meetings.Count == 0)
        {
            return new BehavioralTrendsDto(
                ParticipantName: query.ParticipantName ?? "Unknown",
                AvailableParticipants: [],
                MeetingCount: 0,
                Summary: EmptySummary(),
                TalkTimeTrend: new TrendSeriesDto([]),
                QuestionRatioTrend: new TrendSeriesDto([]),
                InterruptionTrend: new TrendSeriesDto([]),
                SentimentTrend: new TrendSeriesDto([]),
                RedFlagTrend: new RedFlagTrendDto([], []),
                EngagementTrend: new TrendSeriesDto([]));
        }

        // Deserialize behavioral analysis for each meeting
        var meetingAnalyses = new List<(Meeting Meeting, BehavioralAnalysisData Analysis)>();
        foreach (var meeting in meetings)
        {
            var analysis = DeserializeAnalysis(meeting.BehavioralAnalysis!);
            if (analysis is not null)
                meetingAnalyses.Add((meeting, analysis));
        }

        if (meetingAnalyses.Count == 0)
        {
            return new BehavioralTrendsDto(
                ParticipantName: query.ParticipantName ?? "Unknown",
                AvailableParticipants: [],
                MeetingCount: 0,
                Summary: EmptySummary(),
                TalkTimeTrend: new TrendSeriesDto([]),
                QuestionRatioTrend: new TrendSeriesDto([]),
                InterruptionTrend: new TrendSeriesDto([]),
                SentimentTrend: new TrendSeriesDto([]),
                RedFlagTrend: new RedFlagTrendDto([], []),
                EngagementTrend: new TrendSeriesDto([]));
        }

        // Find all unique participants and determine target
        var allParticipants = meetingAnalyses
            .SelectMany(ma => ma.Analysis.SpeakingDynamics.TalkTimeByParticipant)
            .GroupBy(p => p.Participant, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Average(p => p.Percentage))
            .Select(g => g.Key)
            .ToList();

        var targetParticipant = query.ParticipantName
            ?? allParticipants.FirstOrDefault()
            ?? "Unknown";

        // Extract data points
        var talkTimePoints = new List<TrendDataPoint>();
        var questionRatioPoints = new List<TrendDataPoint>();
        var interruptionPoints = new List<TrendDataPoint>();
        var sentimentPoints = new List<TrendDataPoint>();
        var redFlagTotalPoints = new List<TrendDataPoint>();
        var redFlagByType = new Dictionary<string, List<TrendDataPoint>>();
        var engagementPoints = new List<TrendDataPoint>();

        foreach (var (meeting, analysis) in meetingAnalyses)
        {
            var date = meeting.MeetingDate ?? meeting.CreatedAt;
            var label = meeting.Title;

            // Talk time
            var talkTime = analysis.SpeakingDynamics.TalkTimeByParticipant
                .FirstOrDefault(p => string.Equals(p.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase));
            talkTimePoints.Add(new TrendDataPoint(date, talkTime?.Percentage ?? 0, label));

            // Question ratio
            var questionRatio = analysis.SpeakingDynamics.QuestionVsStatementRatio
                .FirstOrDefault(kv => string.Equals(kv.Key, targetParticipant, StringComparison.OrdinalIgnoreCase));
            questionRatioPoints.Add(new TrendDataPoint(date, questionRatio.Value, label));

            // Interruptions (count where this participant was the interrupter)
            var interruptions = analysis.SpeakingDynamics.InterruptionPatterns
                .Where(ip => string.Equals(ip.Interrupter, targetParticipant, StringComparison.OrdinalIgnoreCase))
                .Sum(ip => ip.Count);
            interruptionPoints.Add(new TrendDataPoint(date, interruptions, label));

            // Sentiment
            var sentiment = analysis.SentimentTone.ParticipantSentiments
                .FirstOrDefault(ps => string.Equals(ps.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase));
            sentimentPoints.Add(new TrendDataPoint(date, sentiment?.Score ?? 0, label));

            // Red flags
            var participantFlags = analysis.RedFlags
                .Where(rf => string.Equals(rf.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase))
                .ToList();
            redFlagTotalPoints.Add(new TrendDataPoint(date, participantFlags.Count, label));

            foreach (var group in participantFlags.GroupBy(f => f.Type, StringComparer.OrdinalIgnoreCase))
            {
                var type = group.Key.ToLowerInvariant();
                if (!redFlagByType.TryGetValue(type, out var series))
                {
                    series = [];
                    redFlagByType[type] = series;
                }
                series.Add(new TrendDataPoint(date, group.Count(), label));
            }

            // Engagement
            var engagement = analysis.CommunicationPatterns.EngagementLevels
                .FirstOrDefault(el => string.Equals(el.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase));
            var engagementValue = engagement?.Level.ToLowerInvariant() switch
            {
                "high" => 3.0,
                "medium" => 2.0,
                "low" => 1.0,
                _ => 0.0
            };
            engagementPoints.Add(new TrendDataPoint(date, engagementValue, label));
        }

        // Calculate summary
        var summary = CalculateSummary(
            talkTimePoints, questionRatioPoints, interruptionPoints,
            sentimentPoints, redFlagTotalPoints, engagementPoints);

        return new BehavioralTrendsDto(
            ParticipantName: targetParticipant,
            AvailableParticipants: allParticipants,
            MeetingCount: meetingAnalyses.Count,
            Summary: summary,
            TalkTimeTrend: new TrendSeriesDto(talkTimePoints),
            QuestionRatioTrend: new TrendSeriesDto(questionRatioPoints),
            InterruptionTrend: new TrendSeriesDto(interruptionPoints),
            SentimentTrend: new TrendSeriesDto(sentimentPoints),
            RedFlagTrend: new RedFlagTrendDto(redFlagTotalPoints, redFlagByType),
            EngagementTrend: new TrendSeriesDto(engagementPoints));
    }

    private static DateTimeOffset GetCutoffDate(string range) => range.ToLowerInvariant() switch
    {
        "7d" => DateTimeOffset.UtcNow.AddDays(-7),
        "30d" => DateTimeOffset.UtcNow.AddDays(-30),
        "90d" => DateTimeOffset.UtcNow.AddDays(-90),
        _ => DateTimeOffset.MinValue
    };

    private static BehavioralAnalysisData? DeserializeAnalysis(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<BehavioralAnalysisData>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static TrendSummaryDto CalculateSummary(
        List<TrendDataPoint> talkTime,
        List<TrendDataPoint> questionRatio,
        List<TrendDataPoint> interruptions,
        List<TrendDataPoint> sentiment,
        List<TrendDataPoint> redFlags,
        List<TrendDataPoint> engagement)
    {
        var avgTalkTime = talkTime.Count > 0 ? talkTime.Average(p => p.Value) : 0;
        var avgQuestionRatio = questionRatio.Count > 0 ? questionRatio.Average(p => p.Value) : 0;
        var avgInterruptions = interruptions.Count > 0 ? interruptions.Average(p => p.Value) : 0;
        var avgSentiment = sentiment.Count > 0 ? sentiment.Average(p => p.Value) : 0;
        var totalRedFlags = (int)redFlags.Sum(p => p.Value);

        // Determine dominant engagement level
        var engagementValues = engagement.Select(p => p.Value).ToList();
        var dominantEngagement = engagementValues.Count > 0
            ? engagementValues.Average() switch
            {
                >= 2.5 => "high",
                >= 1.5 => "medium",
                _ => "low"
            }
            : "unknown";

        return new TrendSummaryDto(
            AverageTalkTimePercent: Math.Round(avgTalkTime, 1),
            TalkTimeChange: CalculateChange(talkTime),
            AverageQuestionRatio: Math.Round(avgQuestionRatio, 2),
            QuestionRatioChange: CalculateChange(questionRatio),
            AverageInterruptionCount: Math.Round(avgInterruptions, 1),
            InterruptionChange: CalculateChange(interruptions),
            AverageSentimentScore: Math.Round(avgSentiment, 2),
            SentimentChange: CalculateChange(sentiment),
            TotalRedFlags: totalRedFlags,
            RedFlagChange: CalculateChange(redFlags),
            DominantEngagementLevel: dominantEngagement);
    }

    private static double CalculateChange(List<TrendDataPoint> points)
    {
        if (points.Count < 2) return 0;

        var mid = points.Count / 2;
        var firstHalf = points.Take(mid).Average(p => p.Value);
        var secondHalf = points.Skip(mid).Average(p => p.Value);

        const double epsilon = 1e-6;
        if (Math.Abs(firstHalf) < epsilon) return secondHalf > 0 ? 100 : 0;

        return Math.Round(((secondHalf - firstHalf) / firstHalf) * 100, 1);
    }

    private static TrendSummaryDto EmptySummary() => new(
        AverageTalkTimePercent: 0,
        TalkTimeChange: 0,
        AverageQuestionRatio: 0,
        QuestionRatioChange: 0,
        AverageInterruptionCount: 0,
        InterruptionChange: 0,
        AverageSentimentScore: 0,
        SentimentChange: 0,
        TotalRedFlags: 0,
        RedFlagChange: 0,
        DominantEngagementLevel: "unknown");
}
