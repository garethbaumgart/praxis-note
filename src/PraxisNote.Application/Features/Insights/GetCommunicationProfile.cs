using System.Text.Json;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Insights;

public sealed class GetCommunicationProfile(IMeetingRepository meetingRepository)
{
    public const int MinimumMeetings = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public record Query(Guid UserId, string Range);

    public async Task<CommunicationProfileDto> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var allMeetings = await meetingRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        var cutoff = GetCutoffDate(query.Range);

        var meetings = allMeetings
            .Where(m => (m.Status == MeetingStatus.Ready || m.Status == MeetingStatus.Reviewed)
                        && m.BehavioralAnalysis is not null
                        && (m.MeetingDate ?? m.CreatedAt) >= cutoff)
            .OrderBy(m => m.MeetingDate ?? m.CreatedAt)
            .ToList();

        // Return empty profile with progress if not enough data
        if (meetings.Count < MinimumMeetings)
        {
            return new CommunicationProfileDto(
                PrimaryArchetype: "",
                PrimaryDescription: "",
                SecondaryArchetype: null,
                StyleConsistency: 0,
                MeetingCount: meetings.Count,
                MinimumMeetings: MinimumMeetings,
                HasEnoughData: false,
                Scores: [],
                ContextShifts: [],
                Strengths: [],
                GrowthAreas: []);
        }

        // Deserialize meeting analyses
        var meetingAnalyses = new List<(Meeting Meeting, BehavioralAnalysisData Analysis)>();
        foreach (var meeting in meetings)
        {
            var analysis = DeserializeAnalysis(meeting.BehavioralAnalysis!);
            if (analysis is not null)
                meetingAnalyses.Add((meeting, analysis));
        }

        if (meetingAnalyses.Count < MinimumMeetings)
        {
            return new CommunicationProfileDto(
                PrimaryArchetype: "",
                PrimaryDescription: "",
                SecondaryArchetype: null,
                StyleConsistency: 0,
                MeetingCount: meetingAnalyses.Count,
                MinimumMeetings: MinimumMeetings,
                HasEnoughData: false,
                Scores: [],
                ContextShifts: [],
                Strengths: [],
                GrowthAreas: []);
        }

        // Determine target participant (highest average talk time)
        var targetParticipant = meetingAnalyses
            .SelectMany(ma => ma.Analysis.SpeakingDynamics.TalkTimeByParticipant)
            .GroupBy(p => p.Participant, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Average(p => p.Percentage))
            .Select(g => g.Key)
            .FirstOrDefault() ?? "Unknown";

        // Calculate archetype scores based on behavioral metrics
        var scores = CalculateArchetypeScores(meetingAnalyses, targetParticipant);

        // Determine primary and secondary archetypes
        var sorted = scores.OrderByDescending(s => s.Score).ToList();
        var primary = sorted[0];
        var secondary = sorted.Count > 1 && sorted[1].Score >= 30 ? sorted[1] : null;

        // Calculate style consistency (standard deviation of primary score across meetings)
        var consistency = CalculateStyleConsistency(meetingAnalyses, targetParticipant, primary.Name);

        // Detect context shifts based on meeting size
        var contextShifts = DetectContextShifts(meetingAnalyses, targetParticipant);

        // Determine strengths and growth areas based on profile
        var (strengths, growthAreas) = DetermineStrengthsAndGrowth(scores, primary.Name);

        return new CommunicationProfileDto(
            PrimaryArchetype: primary.Name,
            PrimaryDescription: GetArchetypeDescription(primary.Name),
            SecondaryArchetype: secondary?.Name,
            StyleConsistency: Math.Round(consistency, 1),
            MeetingCount: meetingAnalyses.Count,
            MinimumMeetings: MinimumMeetings,
            HasEnoughData: true,
            Scores: sorted,
            ContextShifts: contextShifts,
            Strengths: strengths,
            GrowthAreas: growthAreas);
    }

    #region Archetype Scoring

    /// <summary>
    /// Calculates scores (0-100) for each of the 6 archetypes based on behavioral metrics.
    /// </summary>
    internal static List<ArchetypeScoreDto> CalculateArchetypeScores(
        List<(Meeting Meeting, BehavioralAnalysisData Analysis)> meetingAnalyses,
        string targetParticipant)
    {
        // Extract aggregate metrics across all meetings
        var avgTalkTime = CalculateAverageTalkTime(meetingAnalyses, targetParticipant);
        var avgQuestionRatio = CalculateAverageQuestionRatio(meetingAnalyses, targetParticipant);
        var avgSentiment = CalculateAverageSentiment(meetingAnalyses, targetParticipant);
        var avgInterruptions = CalculateAverageInterruptions(meetingAnalyses, targetParticipant);
        var avgEngagement = CalculateAverageEngagement(meetingAnalyses, targetParticipant);
        var avgRedFlags = CalculateAverageRedFlags(meetingAnalyses, targetParticipant);
        var avgClarity = meetingAnalyses.Average(ma => ma.Analysis.CommunicationPatterns.OverallClarity);

        // Score each archetype using weighted metric combinations
        var facilitator = CalculateFacilitatorScore(avgTalkTime, avgQuestionRatio, avgEngagement, avgSentiment);
        var driver = CalculateDriverScore(avgTalkTime, avgInterruptions, avgEngagement, avgClarity);
        var observer = CalculateObserverScore(avgTalkTime, avgQuestionRatio, avgInterruptions, avgSentiment);
        var mediator = CalculateMediatorScore(avgSentiment, avgRedFlags, avgQuestionRatio, avgEngagement);
        var challenger = CalculateChallengerScore(avgQuestionRatio, avgInterruptions, avgRedFlags, avgTalkTime);
        var supporter = CalculateSupporterScore(avgSentiment, avgQuestionRatio, avgTalkTime, avgEngagement);

        return
        [
            new ArchetypeScoreDto("Facilitator", Math.Round(facilitator, 1)),
            new ArchetypeScoreDto("Driver", Math.Round(driver, 1)),
            new ArchetypeScoreDto("Observer", Math.Round(observer, 1)),
            new ArchetypeScoreDto("Mediator", Math.Round(mediator, 1)),
            new ArchetypeScoreDto("Challenger", Math.Round(challenger, 1)),
            new ArchetypeScoreDto("Supporter", Math.Round(supporter, 1))
        ];
    }

    /// <summary>
    /// Facilitator: Balanced talk time (30-50%), high question ratio, high engagement, positive sentiment.
    /// </summary>
    internal static double CalculateFacilitatorScore(double avgTalkTime, double avgQuestionRatio, double avgEngagement, double avgSentiment)
    {
        // Talk time 30-50% is optimal → bell curve scoring
        var talkScore = avgTalkTime switch
        {
            >= 30 and <= 50 => 100,
            >= 20 and < 30 => 50 + (avgTalkTime - 20) * 5,
            > 50 and <= 60 => 100 - (avgTalkTime - 50) * 5,
            _ => Math.Max(0, 30 - Math.Abs(avgTalkTime - 40))
        };
        var questionScore = Math.Min(100, avgQuestionRatio * 250); // 0.4 → 100
        var engagementScore = (avgEngagement / 3.0) * 100;
        var sentimentScore = avgSentiment * 100;

        return Clamp((talkScore * 0.30 + questionScore * 0.30 + engagementScore * 0.20 + sentimentScore * 0.20));
    }

    /// <summary>
    /// Driver: High talk time, takes charge, higher interruptions (decisiveness), high clarity.
    /// </summary>
    internal static double CalculateDriverScore(double avgTalkTime, double avgInterruptions, double avgEngagement, double avgClarity)
    {
        var talkScore = Math.Min(100, avgTalkTime * 1.5); // High talk time → high score
        var interruptScore = Math.Min(100, avgInterruptions * 25); // Some interruptions indicate driving
        var engagementScore = (avgEngagement / 3.0) * 100;
        var clarityScore = avgClarity * 100;

        return Clamp((talkScore * 0.35 + interruptScore * 0.20 + engagementScore * 0.20 + clarityScore * 0.25));
    }

    /// <summary>
    /// Observer: Low talk time, few interruptions, high question ratio (listening), positive sentiment.
    /// </summary>
    internal static double CalculateObserverScore(double avgTalkTime, double avgQuestionRatio, double avgInterruptions, double avgSentiment)
    {
        // Lower talk time → higher observer score (inverse)
        var talkScore = Math.Max(0, 100 - avgTalkTime * 1.5);
        var quietScore = Math.Max(0, 100 - avgInterruptions * 30); // Few interruptions
        var questionScore = Math.Min(100, avgQuestionRatio * 200);
        var sentimentScore = avgSentiment * 100;

        return Clamp((talkScore * 0.35 + quietScore * 0.25 + questionScore * 0.20 + sentimentScore * 0.20));
    }

    /// <summary>
    /// Mediator: Very positive sentiment, zero red flags, moderate talk time, some questions.
    /// </summary>
    internal static double CalculateMediatorScore(double avgSentiment, double avgRedFlags, double avgQuestionRatio, double avgEngagement)
    {
        var sentimentScore = avgSentiment * 120; // Extra weight on positivity
        var noRedFlagScore = Math.Max(0, 100 - avgRedFlags * 40); // Fewer flags → higher
        var questionScore = Math.Min(100, avgQuestionRatio * 200);
        var engagementScore = (avgEngagement / 3.0) * 100;

        return Clamp((sentimentScore * 0.35 + noRedFlagScore * 0.25 + questionScore * 0.20 + engagementScore * 0.20));
    }

    /// <summary>
    /// Challenger: High question ratio (probing), some interruptions, more red flags (directness), higher talk time.
    /// </summary>
    internal static double CalculateChallengerScore(double avgQuestionRatio, double avgInterruptions, double avgRedFlags, double avgTalkTime)
    {
        var questionScore = Math.Min(100, avgQuestionRatio * 300); // Deep questioning
        var interruptScore = Math.Min(100, avgInterruptions * 30); // Some interruptions
        var directnessScore = Math.Min(100, avgRedFlags * 30); // Direct communication (can come across as "red flags")
        var talkScore = Math.Min(100, avgTalkTime * 1.3);

        return Clamp((questionScore * 0.30 + interruptScore * 0.25 + directnessScore * 0.15 + talkScore * 0.30));
    }

    /// <summary>
    /// Supporter: High sentiment, lower talk time (lets others lead), few interruptions, moderate engagement.
    /// </summary>
    internal static double CalculateSupporterScore(double avgSentiment, double avgQuestionRatio, double avgTalkTime, double avgEngagement)
    {
        var sentimentScore = avgSentiment * 120;
        var questionScore = Math.Min(100, avgQuestionRatio * 200);
        // Moderate talk time (20-40%) → supportive presence
        var talkScore = avgTalkTime switch
        {
            >= 20 and <= 40 => 100,
            >= 10 and < 20 => 50 + (avgTalkTime - 10) * 5,
            > 40 and <= 55 => 100 - (avgTalkTime - 40) * 4,
            _ => Math.Max(0, 40 - Math.Abs(avgTalkTime - 30))
        };
        var engagementScore = (avgEngagement / 3.0) * 80; // Moderate engagement

        return Clamp((sentimentScore * 0.30 + questionScore * 0.20 + talkScore * 0.25 + engagementScore * 0.25));
    }

    #endregion

    #region Context Shift Detection

    /// <summary>
    /// Detects how communication style changes in different meeting contexts (1:1, small group, large group).
    /// </summary>
    internal static List<ContextShiftDto> DetectContextShifts(
        List<(Meeting Meeting, BehavioralAnalysisData Analysis)> meetingAnalyses,
        string targetParticipant)
    {
        // Categorize meetings by participant count
        var oneOnOne = new List<(Meeting Meeting, BehavioralAnalysisData Analysis)>();
        var smallGroup = new List<(Meeting Meeting, BehavioralAnalysisData Analysis)>();
        var largeGroup = new List<(Meeting Meeting, BehavioralAnalysisData Analysis)>();

        foreach (var ma in meetingAnalyses)
        {
            var participantCount = GetParticipantCount(ma.Meeting, ma.Analysis);

            if (participantCount <= 2)
                oneOnOne.Add(ma);
            else if (participantCount <= 5)
                smallGroup.Add(ma);
            else
                largeGroup.Add(ma);
        }

        var shifts = new List<ContextShiftDto>();

        if (oneOnOne.Count >= 2)
        {
            var scores = CalculateArchetypeScores(oneOnOne, targetParticipant);
            var dominant = scores.OrderByDescending(s => s.Score).First();
            shifts.Add(new ContextShiftDto(
                "1:1 meetings",
                "pi-user",
                dominant.Name,
                GetContextShiftDescription(dominant.Name, "1:1")));
        }

        if (smallGroup.Count >= 2)
        {
            var scores = CalculateArchetypeScores(smallGroup, targetParticipant);
            var dominant = scores.OrderByDescending(s => s.Score).First();
            shifts.Add(new ContextShiftDto(
                "Team meetings",
                "pi-users",
                dominant.Name,
                GetContextShiftDescription(dominant.Name, "team")));
        }

        if (largeGroup.Count >= 2)
        {
            var scores = CalculateArchetypeScores(largeGroup, targetParticipant);
            var dominant = scores.OrderByDescending(s => s.Score).First();
            shifts.Add(new ContextShiftDto(
                "Large groups",
                "pi-sitemap",
                dominant.Name,
                GetContextShiftDescription(dominant.Name, "large")));
        }

        return shifts;
    }

    private static int GetParticipantCount(Meeting meeting, BehavioralAnalysisData analysis)
    {
        // Try attendees field first
        if (!string.IsNullOrWhiteSpace(meeting.Attendees))
        {
            var count = meeting.Attendees.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
            if (count > 0) return count;
        }

        // Fall back to analysis participant count
        return analysis.SpeakingDynamics.TalkTimeByParticipant.Count;
    }

    #endregion

    #region Strengths & Growth

    internal static (List<string> Strengths, List<string> GrowthAreas) DetermineStrengthsAndGrowth(
        List<ArchetypeScoreDto> scores, string primaryArchetype)
    {
        var strengths = primaryArchetype switch
        {
            "Facilitator" => new List<string> { "Balanced airtime", "Thoughtful questions", "High engagement" },
            "Driver" => new List<string> { "Clear direction", "Decisive action", "Strong presence" },
            "Observer" => new List<string> { "Active listening", "Thoughtful contributions", "Calm presence" },
            "Mediator" => new List<string> { "Conflict resolution", "Positive tone", "Bridge-building" },
            "Challenger" => new List<string> { "Critical thinking", "Probing questions", "Intellectual rigor" },
            "Supporter" => new List<string> { "Encouraging tone", "Collaborative spirit", "Builds on ideas" },
            _ => new List<string> { "Engaged participant" }
        };

        // Growth areas are based on the weakest archetypes
        var weakest = scores.OrderBy(s => s.Score).First();
        var growthAreas = weakest.Name switch
        {
            "Facilitator" => new List<string> { "Balance airtime", "Ask more questions", "Include others" },
            "Driver" => new List<string> { "Take more initiative", "Share direct opinions", "Drive decisions" },
            "Observer" => new List<string> { "Listen before responding", "Observe group dynamics", "Reflect more" },
            "Mediator" => new List<string> { "Resolve tension", "Build bridges", "Stay neutral" },
            "Challenger" => new List<string> { "Challenge assumptions", "Push thinking", "Ask hard questions" },
            "Supporter" => new List<string> { "Affirm others' ideas", "Show encouragement", "Build rapport" },
            _ => new List<string> { "Continue developing" }
        };

        return (strengths, growthAreas);
    }

    #endregion

    #region Helper Methods

    private static double CalculateAverageTalkTime(
        List<(Meeting Meeting, BehavioralAnalysisData Analysis)> meetingAnalyses, string targetParticipant)
    {
        var values = meetingAnalyses
            .Select(ma => ma.Analysis.SpeakingDynamics.TalkTimeByParticipant
                .FirstOrDefault(p => string.Equals(p.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase))
                ?.Percentage ?? 0)
            .ToList();
        return values.Count > 0 ? values.Average() : 0;
    }

    private static double CalculateAverageQuestionRatio(
        List<(Meeting Meeting, BehavioralAnalysisData Analysis)> meetingAnalyses, string targetParticipant)
    {
        var values = meetingAnalyses
            .Select(ma =>
            {
                var match = ma.Analysis.SpeakingDynamics.QuestionVsStatementRatio
                    .FirstOrDefault(kv => string.Equals(kv.Key, targetParticipant, StringComparison.OrdinalIgnoreCase));
                return match.Key is not null ? match.Value : 0;
            })
            .ToList();
        return values.Count > 0 ? values.Average() : 0;
    }

    private static double CalculateAverageSentiment(
        List<(Meeting Meeting, BehavioralAnalysisData Analysis)> meetingAnalyses, string targetParticipant)
    {
        var values = meetingAnalyses
            .Select(ma => ma.Analysis.SentimentTone.ParticipantSentiments
                .FirstOrDefault(ps => string.Equals(ps.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase))
                ?.Score ?? 0)
            .ToList();
        return values.Count > 0 ? values.Average() : 0;
    }

    private static double CalculateAverageInterruptions(
        List<(Meeting Meeting, BehavioralAnalysisData Analysis)> meetingAnalyses, string targetParticipant)
    {
        var values = meetingAnalyses
            .Select(ma => (double)ma.Analysis.SpeakingDynamics.InterruptionPatterns
                .Where(ip => string.Equals(ip.Interrupter, targetParticipant, StringComparison.OrdinalIgnoreCase))
                .Sum(ip => ip.Count))
            .ToList();
        return values.Count > 0 ? values.Average() : 0;
    }

    private static double CalculateAverageEngagement(
        List<(Meeting Meeting, BehavioralAnalysisData Analysis)> meetingAnalyses, string targetParticipant)
    {
        var values = meetingAnalyses
            .Select(ma =>
            {
                var engagement = ma.Analysis.CommunicationPatterns.EngagementLevels
                    .FirstOrDefault(el => string.Equals(el.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase));
                return engagement?.Level.ToLowerInvariant() switch
                {
                    "high" => 3.0,
                    "medium" => 2.0,
                    "low" => 1.0,
                    _ => 0.0
                };
            })
            .ToList();
        return values.Count > 0 ? values.Average() : 0;
    }

    private static double CalculateAverageRedFlags(
        List<(Meeting Meeting, BehavioralAnalysisData Analysis)> meetingAnalyses, string targetParticipant)
    {
        var values = meetingAnalyses
            .Select(ma => (double)ma.Analysis.RedFlags
                .Count(rf => string.Equals(rf.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        return values.Count > 0 ? values.Average() : 0;
    }

    private static double CalculateStyleConsistency(
        List<(Meeting Meeting, BehavioralAnalysisData Analysis)> meetingAnalyses,
        string targetParticipant,
        string primaryArchetype)
    {
        if (meetingAnalyses.Count < 2) return 100;

        // Calculate the primary archetype score for each individual meeting
        var perMeetingScores = meetingAnalyses
            .Select(ma =>
            {
                var singleMeeting = new List<(Meeting Meeting, BehavioralAnalysisData Analysis)> { ma };
                var scores = CalculateArchetypeScores(singleMeeting, targetParticipant);
                return scores.First(s => s.Name == primaryArchetype).Score;
            })
            .ToList();

        var mean = perMeetingScores.Average();
        var variance = perMeetingScores.Sum(s => Math.Pow(s - mean, 2)) / perMeetingScores.Count;
        var stdDev = Math.Sqrt(variance);

        // Convert standard deviation to a consistency percentage (lower stdDev = more consistent)
        // StdDev of 0 → 100% consistent, StdDev of 30+ → ~0% consistent
        return Clamp(100 - (stdDev * 3.33));
    }

    private static double Clamp(double value) => Math.Max(0, Math.Min(100, value));

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

    private static string GetArchetypeDescription(string archetype) => archetype switch
    {
        "Facilitator" => "You draw others into the conversation, ask thoughtful questions, and balance airtime across participants. Your meetings tend to be collaborative and inclusive.",
        "Driver" => "You take charge of conversations, set clear direction, and drive toward decisions. Your meetings are structured and outcome-focused.",
        "Observer" => "You listen carefully, contribute selectively, and bring thoughtful insights when you speak. Your presence brings a calm, reflective quality to meetings.",
        "Mediator" => "You bridge differing perspectives, maintain a positive tone, and help resolve tension. Your meetings feel safe and constructive for all participants.",
        "Challenger" => "You push thinking forward with probing questions and aren't afraid to voice disagreement. Your meetings drive deeper analysis and better decisions.",
        "Supporter" => "You encourage others, build on their ideas, and maintain a warm, affirming tone. Your meetings feel collaborative and psychologically safe.",
        _ => "Your communication style is developing."
    };

    private static string GetContextShiftDescription(string archetype, string context) => (archetype, context) switch
    {
        ("Observer", "1:1") => "You listen more and let others lead",
        ("Facilitator", "1:1") => "You draw the other person out with questions",
        ("Driver", "1:1") => "You take a directive approach",
        ("Supporter", "1:1") => "You focus on encouraging the other person",
        ("Mediator", "1:1") => "You maintain a warm, positive connection",
        ("Challenger", "1:1") => "You probe deeper with direct questions",
        ("Driver", "team") => "You take charge of the agenda",
        ("Facilitator", "team") => "You ensure everyone is heard",
        ("Observer", "team") => "You contribute selectively and thoughtfully",
        ("Mediator", "team") => "You bridge different perspectives",
        ("Challenger", "team") => "You push the team to think deeper",
        ("Supporter", "team") => "You build on teammates' ideas",
        ("Facilitator", "large") => "Your default style shines in groups",
        ("Driver", "large") => "You steer the group toward outcomes",
        ("Observer", "large") => "You observe dynamics before contributing",
        ("Mediator", "large") => "You help navigate group tensions",
        ("Challenger", "large") => "You raise important counterpoints",
        ("Supporter", "large") => "You encourage quieter voices",
        _ => $"You tend toward a {archetype} style"
    };

    #endregion
}
