using System.Text.Json;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Insights;

public sealed class GetJohariWindow(IMeetingRepository meetingRepository)
{
    public const int MinimumMeetings = 3;
    public static readonly string[] ValidRanges = ["7d", "30d", "90d", "all"];

    // Quadrant names used consistently across classification, aggregation, and DTOs
    internal const string QuadrantOpen = "Open";
    internal const string QuadrantBlindSpot = "BlindSpot";
    internal const string QuadrantUnknown = "Unknown";

    // Classification thresholds — tune these to adjust sensitivity
    private const double TalkTimeTolerancePercent = 15.0;
    private const double CollaborativeSentimentMin = 0.6;
    private const double NeutralSentimentMin = 0.35;
    private const double NeutralSentimentMax = 0.65;
    private const double TenseSentimentMax = 0.4;
    private const int PartialInterruptionMax = 2;
    private const int MinFreeformReflectionLength = 20;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public record Query(Guid UserId, string Range);

    public async Task<JohariWindowDto> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var allMeetings = await meetingRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        var cutoff = GetCutoffDate(query.Range);

        // Filter to meetings with BOTH analysis and reflection data
        var meetings = allMeetings
            .Where(m => (m.Status == MeetingStatus.Ready || m.Status == MeetingStatus.Reviewed)
                        && m.BehavioralAnalysis is not null
                        && m.ReflectionData is not null
                        && (m.MeetingDate ?? m.CreatedAt) >= cutoff)
            .OrderBy(m => m.MeetingDate ?? m.CreatedAt)
            .ToList();

        if (meetings.Count < MinimumMeetings)
        {
            return CreateEmptyResult(meetings.Count);
        }

        // Deserialize both JSON blobs for each meeting
        var meetingData = new List<(Meeting Meeting, BehavioralAnalysisData Analysis, ReflectionDto Reflection)>();
        foreach (var meeting in meetings)
        {
            var analysis = DeserializeAnalysis(meeting.BehavioralAnalysis!);
            var reflection = DeserializeReflection(meeting.ReflectionData!);
            if (analysis is not null && reflection is not null)
                meetingData.Add((meeting, analysis, reflection));
        }

        if (meetingData.Count < MinimumMeetings)
        {
            return CreateEmptyResult(meetingData.Count);
        }

        // Determine target participant (highest average talk time)
        var targetParticipant = meetingData
            .SelectMany(md => md.Analysis.SpeakingDynamics.TalkTimeByParticipant)
            .GroupBy(p => p.Participant, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Average(p => p.Percentage))
            .Select(g => g.Key)
            .FirstOrDefault() ?? "Unknown";

        // Classify each dimension for each meeting
        var allClassifications = new List<(string Dimension, string Quadrant, string SelfValue, string AiValue)>();
        var hiddenCount = 0;

        foreach (var (meeting, analysis, reflection) in meetingData)
        {
            var actualTalkTime = analysis.SpeakingDynamics.TalkTimeByParticipant
                .FirstOrDefault(p => string.Equals(p.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase))
                ?.Percentage ?? 0;

            var actualEngagement = analysis.CommunicationPatterns.EngagementLevels
                .FirstOrDefault(el => string.Equals(el.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase))
                ?.Level;

            var actualSentiment = analysis.SentimentTone.ParticipantSentiments
                .FirstOrDefault(ps => string.Equals(ps.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase))
                ?.Score ?? 0;

            var actualInterruptions = analysis.SpeakingDynamics.InterruptionPatterns
                .Where(ip => string.Equals(ip.Interrupter, targetParticipant, StringComparison.OrdinalIgnoreCase))
                .Sum(ip => ip.Count);

            allClassifications.Add(("Talk Time",
                ClassifyTalkTime(reflection.SelfAssessedTalkTime, actualTalkTime),
                reflection.SelfAssessedTalkTime?.ToString() ?? "—",
                $"{actualTalkTime:F0}%"));

            allClassifications.Add(("Engagement",
                ClassifyEngagement(reflection.SelfAssessedEngagement, actualEngagement),
                reflection.SelfAssessedEngagement ?? "—",
                actualEngagement ?? "—"));

            allClassifications.Add(("Tone",
                ClassifyTone(reflection.SelfAssessedTone, actualSentiment),
                reflection.SelfAssessedTone ?? "—",
                $"{actualSentiment:F2}"));

            allClassifications.Add(("Interruptions",
                ClassifyInterruptions(reflection.InterruptionAwareness, actualInterruptions),
                reflection.InterruptionAwareness ?? "—",
                actualInterruptions.ToString()));

            hiddenCount += CalculateHiddenCount(reflection);
        }

        // Aggregate counts
        var openCount = allClassifications.Count(c => c.Quadrant == QuadrantOpen);
        var blindCount = allClassifications.Count(c => c.Quadrant == QuadrantBlindSpot);
        var unknownCount = allClassifications.Count(c => c.Quadrant == QuadrantUnknown);
        var total = openCount + blindCount + hiddenCount + unknownCount;

        if (total == 0) total = 1; // Prevent division by zero

        var openPct = (int)Math.Round(openCount * 100.0 / total);
        var blindPct = (int)Math.Round(blindCount * 100.0 / total);
        var hiddenPct = (int)Math.Round(hiddenCount * 100.0 / total);
        var unknownPct = 100 - openPct - blindPct - hiddenPct;

        // Ensure non-negative (rounding could cause -1)
        if (unknownPct < 0)
        {
            unknownPct = 0;
            // Adjust the largest quadrant down by 1
            var max = Math.Max(openPct, Math.Max(blindPct, hiddenPct));
            if (openPct == max) openPct--;
            else if (blindPct == max) blindPct--;
            else hiddenPct--;
        }

        // Calculate open trend (first half vs second half)
        var openTrend = CalculateOpenTrend(meetingData, targetParticipant);

        // Build dimension summary (aggregate per dimension)
        var dimensions = BuildDimensionSummary(allClassifications);

        // Build blind spot details
        var blindSpots = BuildBlindSpotDetails(allClassifications);

        return new JohariWindowDto(
            OpenPercentage: openPct,
            BlindSpotPercentage: blindPct,
            HiddenPercentage: hiddenPct,
            UnknownPercentage: unknownPct,
            MeetingCount: meetingData.Count,
            MinimumMeetings: MinimumMeetings,
            HasEnoughData: true,
            OpenTrend: openTrend,
            Dimensions: dimensions,
            BlindSpots: blindSpots);
    }

    #region Classification Methods

    internal static string ClassifyTalkTime(int? selfAssessed, double actualPercentage)
    {
        if (selfAssessed is null) return QuadrantUnknown;
        return Math.Abs(selfAssessed.Value - actualPercentage) <= TalkTimeTolerancePercent
            ? QuadrantOpen
            : QuadrantBlindSpot;
    }

    internal static string ClassifyEngagement(string? selfAssessed, string? actualLevel)
    {
        if (selfAssessed is null) return QuadrantUnknown;
        if (actualLevel is null) return QuadrantUnknown;

        var selfScore = MapEngagementToScore(selfAssessed);
        var actualScore = MapEngagementLevelToScore(actualLevel);

        // Unrecognized strings map to 0, which only matches other unrecognized strings
        return selfScore == actualScore ? QuadrantOpen : QuadrantBlindSpot;
    }

    internal static string ClassifyTone(string? selfAssessed, double sentimentScore)
    {
        if (selfAssessed is null) return QuadrantUnknown;

        return selfAssessed.ToLowerInvariant() switch
        {
            "collaborative" => sentimentScore >= CollaborativeSentimentMin ? QuadrantOpen : QuadrantBlindSpot,
            "neutral" => sentimentScore >= NeutralSentimentMin && sentimentScore <= NeutralSentimentMax
                ? QuadrantOpen : QuadrantBlindSpot,
            "tense" => sentimentScore <= TenseSentimentMax ? QuadrantOpen : QuadrantBlindSpot,
            _ => QuadrantUnknown
        };
    }

    internal static string ClassifyInterruptions(string? selfAwareness, int actualCount)
    {
        if (selfAwareness is null) return QuadrantUnknown;

        return selfAwareness.ToLowerInvariant() switch
        {
            "yes" => actualCount > 0 ? QuadrantOpen : QuadrantBlindSpot,
            "no" => actualCount == 0 ? QuadrantOpen : QuadrantBlindSpot,
            "partially" => actualCount <= PartialInterruptionMax ? QuadrantOpen : QuadrantBlindSpot,
            _ => QuadrantUnknown
        };
    }

    internal static int CalculateHiddenCount(ReflectionDto reflection)
    {
        var count = 0;

        // Freeform reflection represents private self-knowledge not in analysis
        if (!string.IsNullOrWhiteSpace(reflection.FreeformReflection)
            && reflection.FreeformReflection.Length > MinFreeformReflectionLength)
            count++;

        return count;
    }

    #endregion

    #region Aggregation Helpers

    private static double? CalculateOpenTrend(
        List<(Meeting Meeting, BehavioralAnalysisData Analysis, ReflectionDto Reflection)> meetingData,
        string targetParticipant)
    {
        if (meetingData.Count < 4) return null; // Need at least 2 per half

        var midpoint = meetingData.Count / 2;
        var firstHalf = meetingData.Take(midpoint).ToList();
        var secondHalf = meetingData.Skip(midpoint).ToList();

        var firstOpenRate = CalculateOpenRate(firstHalf, targetParticipant);
        var secondOpenRate = CalculateOpenRate(secondHalf, targetParticipant);

        return Math.Round(secondOpenRate - firstOpenRate, 1);
    }

    private static double CalculateOpenRate(
        List<(Meeting Meeting, BehavioralAnalysisData Analysis, ReflectionDto Reflection)> subset,
        string targetParticipant)
    {
        var total = 0;
        var open = 0;

        foreach (var (_, analysis, reflection) in subset)
        {
            var actualTalkTime = analysis.SpeakingDynamics.TalkTimeByParticipant
                .FirstOrDefault(p => string.Equals(p.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase))
                ?.Percentage ?? 0;
            var actualEngagement = analysis.CommunicationPatterns.EngagementLevels
                .FirstOrDefault(el => string.Equals(el.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase))
                ?.Level;
            var actualSentiment = analysis.SentimentTone.ParticipantSentiments
                .FirstOrDefault(ps => string.Equals(ps.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase))
                ?.Score ?? 0;
            var actualInterruptions = analysis.SpeakingDynamics.InterruptionPatterns
                .Where(ip => string.Equals(ip.Interrupter, targetParticipant, StringComparison.OrdinalIgnoreCase))
                .Sum(ip => ip.Count);

            if (ClassifyTalkTime(reflection.SelfAssessedTalkTime, actualTalkTime) == QuadrantOpen) open++;
            if (ClassifyEngagement(reflection.SelfAssessedEngagement, actualEngagement) == QuadrantOpen) open++;
            if (ClassifyTone(reflection.SelfAssessedTone, actualSentiment) == QuadrantOpen) open++;
            if (ClassifyInterruptions(reflection.InterruptionAwareness, actualInterruptions) == QuadrantOpen) open++;
            total += 4;
        }

        return total > 0 ? open * 100.0 / total : 0;
    }

    private static List<JohariDimensionDto> BuildDimensionSummary(
        List<(string Dimension, string Quadrant, string SelfValue, string AiValue)> allClassifications)
    {
        return allClassifications
            .GroupBy(c => c.Dimension)
            .Select(g =>
            {
                // Most frequent quadrant for this dimension
                var dominantQuadrant = g
                    .GroupBy(c => c.Quadrant)
                    .OrderByDescending(q => q.Count())
                    .First().Key;

                var lastEntry = g.Last();
                var explanation = GetDimensionExplanation(g.Key, dominantQuadrant, g.Count(c => c.Quadrant == QuadrantOpen), g.Count());

                return new JohariDimensionDto(
                    Name: g.Key,
                    Quadrant: dominantQuadrant,
                    SelfValue: lastEntry.SelfValue,
                    AiValue: lastEntry.AiValue,
                    Explanation: explanation);
            })
            .ToList();
    }

    private static List<BlindSpotDetailDto> BuildBlindSpotDetails(
        List<(string Dimension, string Quadrant, string SelfValue, string AiValue)> allClassifications)
    {
        return allClassifications
            .Where(c => c.Quadrant == QuadrantBlindSpot)
            .GroupBy(c => c.Dimension)
            .Select(g => new BlindSpotDetailDto(
                Dimension: g.Key,
                Description: GetBlindSpotDescription(g.Key, g.ToList()),
                MeetingCount: g.Count()))
            .OrderByDescending(b => b.MeetingCount)
            .ToList();
    }

    #endregion

    #region Helper Methods

    private static int MapEngagementToScore(string engagement) => engagement.ToLowerInvariant() switch
    {
        "highly engaged" => 3,
        "moderate" => 2,
        "disengaged" => 1,
        _ => 0
    };

    private static int MapEngagementLevelToScore(string level) => level.ToLowerInvariant() switch
    {
        "high" => 3,
        "medium" => 2,
        "low" => 1,
        _ => 0
    };

    private static string GetDimensionExplanation(string dimension, string quadrant, int openCount, int totalCount)
    {
        var rate = totalCount > 0 ? $"{openCount}/{totalCount}" : "0/0";
        return (dimension, quadrant) switch
        {
            ("Talk Time", "Open") => $"Your talk time estimates aligned with AI analysis in {rate} meetings",
            ("Talk Time", "BlindSpot") => $"Your talk time perception differed from AI measurement in most meetings ({rate} aligned)",
            ("Engagement", "Open") => $"Your engagement self-assessment matched AI detection in {rate} meetings",
            ("Engagement", "BlindSpot") => $"AI detected different engagement levels than you reported ({rate} aligned)",
            ("Tone", "Open") => $"Your tone perception aligned with sentiment analysis in {rate} meetings",
            ("Tone", "BlindSpot") => $"Sentiment analysis differed from your tone assessment ({rate} aligned)",
            ("Interruptions", "Open") => $"Your interruption awareness matched AI detection in {rate} meetings",
            ("Interruptions", "BlindSpot") => $"AI detected interruptions you weren't aware of ({rate} aligned)",
            _ => $"Aligned in {rate} meetings"
        };
    }

    private static string GetBlindSpotDescription(string dimension,
        List<(string Dimension, string Quadrant, string SelfValue, string AiValue)> entries)
    {
        var latest = entries.Last();
        return dimension switch
        {
            "Talk Time" => $"You estimated ~{latest.SelfValue}% talk time, but AI measured {latest.AiValue}",
            "Engagement" => $"You rated yourself as \"{latest.SelfValue}\", but AI detected \"{latest.AiValue}\" engagement",
            "Tone" => $"You perceived the tone as \"{latest.SelfValue}\", but sentiment scored {latest.AiValue}",
            "Interruptions" => $"You indicated \"{latest.SelfValue}\" interruption awareness, but AI detected {latest.AiValue}",
            _ => $"Self-assessment and AI analysis differ for {dimension}"
        };
    }

    private static JohariWindowDto CreateEmptyResult(int meetingCount) => new(
        OpenPercentage: 0,
        BlindSpotPercentage: 0,
        HiddenPercentage: 0,
        UnknownPercentage: 0,
        MeetingCount: meetingCount,
        MinimumMeetings: MinimumMeetings,
        HasEnoughData: false,
        OpenTrend: null,
        Dimensions: [],
        BlindSpots: []);

    private static DateTimeOffset GetCutoffDate(string range) => range switch
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

    private static ReflectionDto? DeserializeReflection(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ReflectionDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    #endregion
}
