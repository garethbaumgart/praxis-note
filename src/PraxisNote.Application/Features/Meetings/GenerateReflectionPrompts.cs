using System.Text.Json;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class GenerateReflectionPrompts(IMeetingRepository meetingRepository)
{
    public record Query(Guid MeetingId, Guid UserId);

    public record Result(IReadOnlyList<ReflectionPromptDto> Prompts);

    public async Task<Result?> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(query.MeetingId, cancellationToken);

        if (meeting is null || meeting.UserId != query.UserId)
            return null;

        var prompts = GeneratePrompts(meeting.BehavioralAnalysis);

        return new Result(prompts);
    }

    internal static IReadOnlyList<ReflectionPromptDto> GeneratePrompts(string? behavioralAnalysisJson)
    {
        var prompts = new List<ReflectionPromptDto>();

        if (string.IsNullOrWhiteSpace(behavioralAnalysisJson))
        {
            return GenerateGenericPrompts();
        }

        BehavioralAnalysisData? analysis;
        try
        {
            analysis = JsonSerializer.Deserialize<BehavioralAnalysisData>(behavioralAnalysisJson, JsonOptions);
        }
        catch
        {
            return GenerateGenericPrompts();
        }

        if (analysis is null)
            return GenerateGenericPrompts();

        // Rule: Dominant speaker (anyone with > 50% talk time)
        var dominantSpeaker = analysis.SpeakingDynamics?.TalkTimeByParticipant?
            .FirstOrDefault(p => p.Percentage > 50);
        if (dominantSpeaker is not null)
        {
            prompts.Add(new ReflectionPromptDto(
                "talk-time-dominant",
                "talk-time",
                $"You spoke for {dominantSpeaker.Percentage:F0}% of the meeting. How would you rate your talk time?",
                ["Too Much", "About Right", "Too Little"]));
        }

        // Rule: Interruptions >= 2
        var totalInterruptions = analysis.SpeakingDynamics?.InterruptionPatterns?
            .Sum(p => p.Count) ?? 0;
        if (totalInterruptions >= 2)
        {
            prompts.Add(new ReflectionPromptDto(
                "interruptions-awareness",
                "interruptions",
                $"The analysis detected {totalInterruptions} interruption(s). Were you aware of these during the meeting?",
                ["Yes", "Partially", "No"]));
        }

        // Rule: Negative sentiment detected
        var negativeSentiment = analysis.SentimentTone?.ParticipantSentiments?
            .Any(s => string.Equals(s.Sentiment, "negative", StringComparison.OrdinalIgnoreCase)) ?? false;
        if (negativeSentiment)
        {
            prompts.Add(new ReflectionPromptDto(
                "tone-negative",
                "tone",
                "The analysis detected a negative tone. How would you describe the meeting atmosphere?",
                ["Collaborative", "Neutral", "Tense"]));
        }

        // Rule: Low engagement detected
        var lowEngagement = analysis.CommunicationPatterns?.EngagementLevels?
            .Any(e => string.Equals(e.Level, "low", StringComparison.OrdinalIgnoreCase)) ?? false;
        if (lowEngagement)
        {
            prompts.Add(new ReflectionPromptDto(
                "engagement-low",
                "engagement",
                "Some participants showed low engagement. How engaged did you feel during this meeting?",
                ["Highly Engaged", "Moderate", "Disengaged"]));
        }

        // Rule: Red flags with high severity
        var highSeverityFlags = analysis.RedFlags?
            .Where(f => string.Equals(f.Severity, "high", StringComparison.OrdinalIgnoreCase))
            .ToList() ?? [];
        if (highSeverityFlags.Count > 0)
        {
            var flag = highSeverityFlags[0];
            prompts.Add(new ReflectionPromptDto(
                "red-flag-high",
                "general",
                $"A communication concern was flagged: \"{flag.Description}\". How would you reflect on this?",
                ["I'll be more mindful", "The context was different than it appears", "I disagree with this assessment"]));
        }

        // Rule: Tone shifts detected
        var toneShifts = analysis.SentimentTone?.ToneShifts ?? [];
        if (toneShifts.Count > 0)
        {
            var shift = toneShifts[0];
            prompts.Add(new ReflectionPromptDto(
                "tone-shift",
                "tone",
                $"There was a tone shift during the meeting ({shift.From} to {shift.To}). What prompted this change?",
                []));
        }

        // Rule: Always include general reflection (freeform)
        prompts.Add(new ReflectionPromptDto(
            "general-improvement",
            "general",
            "What is one thing you would do differently in your next meeting?",
            []));

        // Rule: Positive reinforcement for high engagement
        var highEngagement = analysis.CommunicationPatterns?.EngagementLevels?
            .Any(e => string.Equals(e.Level, "high", StringComparison.OrdinalIgnoreCase)) ?? false;
        var positiveSentiment = analysis.SentimentTone?.ParticipantSentiments?
            .Any(s => string.Equals(s.Sentiment, "positive", StringComparison.OrdinalIgnoreCase)) ?? false;
        if (highEngagement || positiveSentiment)
        {
            prompts.Add(new ReflectionPromptDto(
                "positive-reinforcement",
                "engagement",
                "Your engagement was notably high. What contributed to your active participation?",
                ["The topic was important to me", "I felt comfortable with the group", "I was well prepared"]));
        }

        return prompts;
    }

    private static IReadOnlyList<ReflectionPromptDto> GenerateGenericPrompts()
    {
        return
        [
            new ReflectionPromptDto(
                "generic-talk-time",
                "talk-time",
                "How would you estimate your share of the conversation?",
                ["Too Much", "About Right", "Too Little"]),
            new ReflectionPromptDto(
                "generic-engagement",
                "engagement",
                "How engaged did you feel during this meeting?",
                ["Highly Engaged", "Moderate", "Disengaged"]),
            new ReflectionPromptDto(
                "generic-tone",
                "tone",
                "How would you describe the overall tone of the meeting?",
                ["Collaborative", "Neutral", "Tense"]),
            new ReflectionPromptDto(
                "general-improvement",
                "general",
                "What is one thing you would do differently in your next meeting?",
                []),
        ];
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
