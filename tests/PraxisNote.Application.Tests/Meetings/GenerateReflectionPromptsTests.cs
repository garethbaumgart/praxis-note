using System.Text.Json;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;

namespace PraxisNote.Application.Tests.Meetings;

public sealed class GenerateReflectionPromptsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #region Generic Prompts (no behavioral analysis)

    [Fact]
    public void GeneratePrompts_NullAnalysis_ReturnsGenericPrompts()
    {
        var prompts = GenerateReflectionPrompts.GeneratePrompts(null);

        Assert.Equal(4, prompts.Count);
        Assert.Contains(prompts, p => p.PromptId == "generic-talk-time");
        Assert.Contains(prompts, p => p.PromptId == "generic-engagement");
        Assert.Contains(prompts, p => p.PromptId == "generic-tone");
        Assert.Contains(prompts, p => p.PromptId == "general-improvement");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void GeneratePrompts_EmptyOrWhitespaceAnalysis_ReturnsGenericPrompts(string json)
    {
        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        Assert.Equal(4, prompts.Count);
        Assert.Contains(prompts, p => p.PromptId == "generic-talk-time");
    }

    [Fact]
    public void GeneratePrompts_InvalidJson_ReturnsGenericPrompts()
    {
        var prompts = GenerateReflectionPrompts.GeneratePrompts("not valid json{{{");

        Assert.Equal(4, prompts.Count);
        Assert.Contains(prompts, p => p.PromptId == "generic-talk-time");
    }

    #endregion

    #region Talk Time Rule

    [Fact]
    public void GeneratePrompts_DominantSpeakerOver50Percent_AddsTalkTimePrompt()
    {
        var analysis = CreateAnalysis(speakingPercentages: [("Alice", 65), ("Bob", 35)]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        var talkTimePrompt = prompts.FirstOrDefault(p => p.PromptId == "talk-time-dominant");
        Assert.NotNull(talkTimePrompt);
        Assert.Equal("talk-time", talkTimePrompt.Category);
        Assert.Contains("65", talkTimePrompt.PromptText);
        Assert.Equal(["Too Much", "About Right", "Too Little"], talkTimePrompt.QuickOptions);
    }

    [Fact]
    public void GeneratePrompts_NoParticipantOver50Percent_NoTalkTimePrompt()
    {
        var analysis = CreateAnalysis(speakingPercentages: [("Alice", 40), ("Bob", 35), ("Carol", 25)]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        Assert.DoesNotContain(prompts, p => p.PromptId == "talk-time-dominant");
    }

    #endregion

    #region Interruptions Rule

    [Fact]
    public void GeneratePrompts_TwoOrMoreInterruptions_AddsInterruptionPrompt()
    {
        var analysis = CreateAnalysis(
            interruptions: [("Alice", "Bob", 2)]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        var prompt = prompts.FirstOrDefault(p => p.PromptId == "interruptions-awareness");
        Assert.NotNull(prompt);
        Assert.Equal("interruptions", prompt.Category);
        Assert.Equal(["Yes", "Partially", "No"], prompt.QuickOptions);
    }

    [Fact]
    public void GeneratePrompts_LessThanTwoInterruptions_NoInterruptionPrompt()
    {
        var analysis = CreateAnalysis(
            interruptions: [("Alice", "Bob", 1)]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        Assert.DoesNotContain(prompts, p => p.PromptId == "interruptions-awareness");
    }

    [Fact]
    public void GeneratePrompts_InterruptionsSummedAcrossPatterns_AddsPromptWhenTotalReachesThreshold()
    {
        var analysis = CreateAnalysis(
            interruptions: [("Alice", "Bob", 1), ("Carol", "Bob", 1)]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        Assert.Contains(prompts, p => p.PromptId == "interruptions-awareness");
    }

    #endregion

    #region Negative Sentiment Rule

    [Fact]
    public void GeneratePrompts_NegativeSentiment_AddsTonePrompt()
    {
        var analysis = CreateAnalysis(
            sentiments: [("Alice", "negative", 0.3)]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        var prompt = prompts.FirstOrDefault(p => p.PromptId == "tone-negative");
        Assert.NotNull(prompt);
        Assert.Equal("tone", prompt.Category);
        Assert.Equal(["Collaborative", "Neutral", "Tense"], prompt.QuickOptions);
    }

    [Fact]
    public void GeneratePrompts_NoNegativeSentiment_NoTonePrompt()
    {
        var analysis = CreateAnalysis(
            sentiments: [("Alice", "positive", 0.8)]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        Assert.DoesNotContain(prompts, p => p.PromptId == "tone-negative");
    }

    #endregion

    #region Low Engagement Rule

    [Fact]
    public void GeneratePrompts_LowEngagement_AddsEngagementPrompt()
    {
        var analysis = CreateAnalysis(
            engagementLevels: [("Bob", "low")]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        var prompt = prompts.FirstOrDefault(p => p.PromptId == "engagement-low");
        Assert.NotNull(prompt);
        Assert.Equal("engagement", prompt.Category);
        Assert.Equal(["Highly Engaged", "Moderate", "Disengaged"], prompt.QuickOptions);
    }

    [Fact]
    public void GeneratePrompts_NoLowEngagement_NoEngagementPrompt()
    {
        var analysis = CreateAnalysis(
            engagementLevels: [("Bob", "high")]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        Assert.DoesNotContain(prompts, p => p.PromptId == "engagement-low");
    }

    #endregion

    #region Red Flags Rule

    [Fact]
    public void GeneratePrompts_HighSeverityRedFlag_AddsRedFlagPrompt()
    {
        var analysis = CreateAnalysis(
            redFlags: [("evasive", "Alice", "Avoided the question", "During budget discussion", "high")]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        var prompt = prompts.FirstOrDefault(p => p.PromptId == "red-flag-high");
        Assert.NotNull(prompt);
        Assert.Contains("Avoided the question", prompt.PromptText);
    }

    [Fact]
    public void GeneratePrompts_LowSeverityRedFlag_NoRedFlagPrompt()
    {
        var analysis = CreateAnalysis(
            redFlags: [("hedging", "Alice", "Used uncertain language", "General discussion", "low")]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        Assert.DoesNotContain(prompts, p => p.PromptId == "red-flag-high");
    }

    #endregion

    #region Tone Shift Rule

    [Fact]
    public void GeneratePrompts_ToneShift_AddsToneShiftPrompt()
    {
        var analysis = CreateAnalysis(
            toneShifts: [("10:05", "Discussion became heated", "collaborative", "defensive")]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        var prompt = prompts.FirstOrDefault(p => p.PromptId == "tone-shift");
        Assert.NotNull(prompt);
        Assert.Contains("collaborative", prompt.PromptText);
        Assert.Contains("defensive", prompt.PromptText);
        Assert.Empty(prompt.QuickOptions); // Freeform
    }

    #endregion

    #region General Improvement Prompt

    [Fact]
    public void GeneratePrompts_WithValidAnalysis_AlwaysIncludesGeneralImprovement()
    {
        var analysis = CreateAnalysis();
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        Assert.Contains(prompts, p => p.PromptId == "general-improvement");
    }

    #endregion

    #region Positive Reinforcement Rule

    [Fact]
    public void GeneratePrompts_HighEngagement_AddsPositiveReinforcement()
    {
        var analysis = CreateAnalysis(
            engagementLevels: [("Alice", "high")]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        Assert.Contains(prompts, p => p.PromptId == "positive-reinforcement");
    }

    [Fact]
    public void GeneratePrompts_PositiveSentiment_AddsPositiveReinforcement()
    {
        var analysis = CreateAnalysis(
            sentiments: [("Alice", "positive", 0.9)]);
        var json = Serialize(analysis);

        var prompts = GenerateReflectionPrompts.GeneratePrompts(json);

        Assert.Contains(prompts, p => p.PromptId == "positive-reinforcement");
    }

    #endregion

    #region Helpers

    private static string Serialize(BehavioralAnalysisData data)
        => JsonSerializer.Serialize(data, JsonOptions);

    private static BehavioralAnalysisData CreateAnalysis(
        (string name, double pct)[]? speakingPercentages = null,
        (string interrupter, string interrupted, int count)[]? interruptions = null,
        (string participant, string sentiment, double score)[]? sentiments = null,
        (string timestamp, string description, string from, string to)[]? toneShifts = null,
        (string participant, string level)[]? engagementLevels = null,
        (string type, string participant, string description, string context, string severity)[]? redFlags = null)
    {
        var talkTime = speakingPercentages?
            .Select(s => new ParticipantTalkTime(s.name, s.pct, $"{s.pct}%"))
            .ToList() ?? [new ParticipantTalkTime("User", 30, "30%"), new ParticipantTalkTime("Other", 70, "70%")];

        var interruptionPatterns = interruptions?
            .Select(i => new InterruptionPattern(i.interrupter, i.interrupted, i.count))
            .ToList() ?? [];

        var participantSentiments = sentiments?
            .Select(s => new ParticipantSentiment(s.participant, s.sentiment, s.score))
            .ToList() ?? [new ParticipantSentiment("User", "neutral", 0.5)];

        var shifts = toneShifts?
            .Select(t => new ToneShift(t.timestamp, t.description, t.from, t.to))
            .ToList() ?? [];

        var engagement = engagementLevels?
            .Select(e => new ParticipantEngagement(e.participant, e.level, []))
            .ToList() ?? [new ParticipantEngagement("User", "moderate", [])];

        var flags = redFlags?
            .Select(r => new RedFlag(r.type, r.participant, r.description, r.context, r.severity))
            .ToList() ?? [];

        return new BehavioralAnalysisData(
            new SpeakingDynamics(talkTime, interruptionPatterns, new Dictionary<string, double>()),
            new SentimentTone(participantSentiments, shifts, []),
            new CommunicationPatterns(0.8, [], engagement),
            flags);
    }

    #endregion
}
