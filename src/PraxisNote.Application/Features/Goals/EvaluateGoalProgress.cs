using System.Text.Json;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.BehavioralGoals;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Goals;

public sealed class EvaluateGoalProgress(
    IBehavioralGoalRepository goalRepository,
    IMeetingRepository meetingRepository)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public record Query(Guid UserId);

    public async Task<IReadOnlyList<GoalProgressDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var goals = await goalRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        if (goals.Count == 0)
            return [];

        var allMeetings = await meetingRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);

        var recentMeetings = allMeetings
            .Where(m => (m.Status == MeetingStatus.Ready || m.Status == MeetingStatus.Reviewed)
                        && m.BehavioralAnalysis is not null
                        && (m.MeetingDate ?? m.CreatedAt) >= cutoff)
            .OrderBy(m => m.MeetingDate ?? m.CreatedAt)
            .ToList();

        var meetingAnalyses = new List<(Meeting Meeting, BehavioralAnalysisData Analysis)>();
        foreach (var meeting in recentMeetings)
        {
            var analysis = DeserializeAnalysis(meeting.BehavioralAnalysis!);
            if (analysis is not null)
                meetingAnalyses.Add((meeting, analysis));
        }

        // Determine target participant (highest average talk time — same logic as GetInsightsSummary)
        var targetParticipant = meetingAnalyses
            .SelectMany(ma => ma.Analysis.SpeakingDynamics.TalkTimeByParticipant)
            .GroupBy(p => p.Participant, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Average(p => p.Percentage))
            .Select(g => g.Key)
            .FirstOrDefault() ?? "Unknown";

        var results = new List<GoalProgressDto>();

        foreach (var goal in goals)
        {
            var perMeetingValues = ExtractMetricValues(goal.MetricType, meetingAnalyses, targetParticipant);
            var currentValue = perMeetingValues.Count > 0 ? perMeetingValues.Average() : (double?)null;
            var met = currentValue.HasValue && goal.Evaluate(currentValue.Value);

            // Streak: count consecutive recent meetings where goal was met (most recent first)
            var streak = 0;
            for (var i = perMeetingValues.Count - 1; i >= 0; i--)
            {
                if (goal.Evaluate(perMeetingValues[i]))
                    streak++;
                else
                    break;
            }

            // Last N meeting pass/fail for dot track
            var recentResults = perMeetingValues
                .TakeLast(8)
                .Select(v => goal.Evaluate(v))
                .ToList();

            results.Add(new GoalProgressDto(
                GoalId: goal.Id,
                Title: goal.Title,
                MetricType: goal.MetricType.ToString(),
                Operator: goal.Operator.ToString(),
                TargetValue: goal.TargetValue,
                TargetValueUpper: goal.TargetValueUpper,
                IsActive: goal.IsActive,
                CurrentValue: currentValue.HasValue ? Math.Round(currentValue.Value, 2) : null,
                IsMet: met,
                Streak: streak,
                MeetingsEvaluated: perMeetingValues.Count,
                RecentResults: recentResults));
        }

        return results;
    }

    private static List<double> ExtractMetricValues(
        MetricType metricType,
        List<(Meeting Meeting, BehavioralAnalysisData Analysis)> meetingAnalyses,
        string targetParticipant)
    {
        return metricType switch
        {
            MetricType.TalkTimePercentage => meetingAnalyses
                .Select(ma => ma.Analysis.SpeakingDynamics.TalkTimeByParticipant
                    .FirstOrDefault(p => string.Equals(p.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase))
                    ?.Percentage ?? 0)
                .ToList(),

            MetricType.QuestionRatio => meetingAnalyses
                .Select(ma =>
                {
                    var match = ma.Analysis.SpeakingDynamics.QuestionVsStatementRatio
                        .FirstOrDefault(kv => string.Equals(kv.Key, targetParticipant, StringComparison.OrdinalIgnoreCase));
                    return match.Key is not null ? match.Value : 0;
                })
                .ToList(),

            MetricType.InterruptionCount => meetingAnalyses
                .Select(ma => (double)ma.Analysis.SpeakingDynamics.InterruptionPatterns
                    .Where(ip => string.Equals(ip.Interrupter, targetParticipant, StringComparison.OrdinalIgnoreCase))
                    .Sum(ip => ip.Count))
                .ToList(),

            MetricType.SentimentScore => meetingAnalyses
                .Select(ma => ma.Analysis.SentimentTone.ParticipantSentiments
                    .FirstOrDefault(ps => string.Equals(ps.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase))
                    ?.Score ?? 0)
                .ToList(),

            MetricType.RedFlagCount => meetingAnalyses
                .Select(ma => (double)ma.Analysis.RedFlags
                    .Count(rf => string.Equals(rf.Participant, targetParticipant, StringComparison.OrdinalIgnoreCase)))
                .ToList(),

            _ => []
        };
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

public record GoalProgressDto(
    Guid GoalId,
    string Title,
    string MetricType,
    string Operator,
    double TargetValue,
    double? TargetValueUpper,
    bool IsActive,
    double? CurrentValue,
    bool IsMet,
    int Streak,
    int MeetingsEvaluated,
    List<bool> RecentResults);
