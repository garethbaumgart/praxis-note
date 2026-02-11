using System.Text.Json;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Insights;

public sealed class GetInsightsSummary(IMeetingRepository meetingRepository)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public record Query(Guid UserId, Guid ProfileId);

    public async Task<InsightsSummaryDto?> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var allMeetings = await meetingRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);

        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);

        var meetings = allMeetings
            .Where(m => (m.Status == MeetingStatus.Ready || m.Status == MeetingStatus.Reviewed)
                        && m.BehavioralAnalysis is not null
                        && !m.ExcludeFromInsights
                        && (m.MeetingDate ?? m.CreatedAt) >= cutoff)
            .OrderBy(m => m.MeetingDate ?? m.CreatedAt)
            .ToList();

        if (meetings.Count == 0)
            return null;

        var meetingAnalyses = new List<(Meeting Meeting, BehavioralAnalysisData Analysis)>();
        foreach (var meeting in meetings)
        {
            var analysis = DeserializeAnalysis(meeting.BehavioralAnalysis!);
            if (analysis is not null)
                meetingAnalyses.Add((meeting, analysis));
        }

        if (meetingAnalyses.Count == 0)
            return null;

        // Determine target participant (highest average talk time)
        var targetParticipant = meetingAnalyses
            .SelectMany(ma => ma.Analysis.SpeakingDynamics.TalkTimeByParticipant)
            .GroupBy(p => p.Participant, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Average(p => p.Percentage))
            .Select(g => g.Key)
            .FirstOrDefault() ?? "Unknown";

        // Extract talk-time per meeting for sparkline and headline
        var talkTimePoints = meetingAnalyses
            .Select(ma => ma.Analysis.SpeakingDynamics.TalkTimeByParticipant
                .FirstOrDefault(p => string.Equals(p.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase))
                ?.Percentage ?? 0)
            .ToList();

        var avgTalkTime = talkTimePoints.Count > 0 ? talkTimePoints.Average() : 0;
        var talkTimeChange = CalculateChange(talkTimePoints);

        // Question ratio — only include meetings where the participant has a ratio entry
        var questionRatioValues = meetingAnalyses
            .Select(ma =>
            {
                var match = ma.Analysis.SpeakingDynamics.QuestionVsStatementRatio
                    .FirstOrDefault(kv => string.Equals(kv.Key, targetParticipant, StringComparison.OrdinalIgnoreCase));
                return match.Key is not null ? (double?)match.Value : null;
            })
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        var avgQuestionRatio = questionRatioValues.Count > 0 ? questionRatioValues.Average() : 0;
        var questionRatioChange = CalculateChange(questionRatioValues);

        // Red flags
        var redFlagCounts = meetingAnalyses
            .Select(ma => (double)ma.Analysis.RedFlags
                .Count(rf => string.Equals(rf.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var totalRedFlags = redFlagCounts.Sum();
        var redFlagChange = CalculateChange(redFlagCounts);

        // Generate nudge text
        var nudgeText = GenerateNudge(talkTimeChange, questionRatioChange, redFlagChange, avgTalkTime);

        // Sparkline: last 8 talk-time values (or fewer)
        var sparkline = talkTimePoints
            .TakeLast(8)
            .Select(v => Math.Round(v, 1))
            .ToList();

        return new InsightsSummaryDto(
            MeetingCount: meetingAnalyses.Count,
            ParticipantName: targetParticipant,
            Headline: new InsightsHeadlineMetric(
                Label: "Talk Time",
                Value: Math.Round(avgTalkTime, 1),
                Change: Math.Round(talkTimeChange, 1),
                Unit: "%"),
            QuestionRatio: new InsightsSecondaryMetric(
                Label: "Questions",
                Value: Math.Round(avgQuestionRatio, 2),
                Change: Math.Round(questionRatioChange, 1)),
            RedFlags: new InsightsSecondaryMetric(
                Label: "Red Flags",
                Value: totalRedFlags,
                Change: Math.Round(redFlagChange, 1)),
            NudgeText: nudgeText,
            SparklineValues: sparkline);
    }

    private static double CalculateChange(List<double> values)
    {
        if (values.Count < 2) return 0;

        var mid = values.Count / 2;
        var firstHalf = values.Take(mid).Average();
        var secondHalf = values.Skip(mid).Average();

        const double epsilon = 1e-6;
        if (Math.Abs(firstHalf) < epsilon) return secondHalf > 0 ? 100 : 0;

        return ((secondHalf - firstHalf) / firstHalf) * 100;
    }

    private static string? GenerateNudge(
        double talkTimeChange, double questionRatioChange, double redFlagChange, double avgTalkTime)
    {
        // Prioritize the most notable positive trend
        if (questionRatioChange > 10)
            return "Your question ratio improved recently \u2014 keep it up!";

        if (talkTimeChange < -10 && avgTalkTime < 45)
            return "Talk time trending down \u2014 you\u2019re leaving more room for others.";

        if (redFlagChange < -20)
            return "Fewer red flags detected \u2014 your communication is getting clearer.";

        if (talkTimeChange > 15)
            return "Your talk time increased recently \u2014 check if you\u2019re balancing airtime.";

        if (redFlagChange > 20)
            return "More red flags detected recently \u2014 review your latest meetings for patterns.";

        return null;
    }

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
}
