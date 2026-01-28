using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class ClaudeMeetingAnalyzer : IMeetingAnalyzer
{
    private readonly MeetingAnalysisSettings _settings;
    private readonly ILogger<ClaudeMeetingAnalyzer> _logger;
    private readonly AnthropicClient? _client;

    private const string AnalysisPrompt = """
        Analyze this meeting transcript and provide a comprehensive JSON response.

        CONTENT ANALYSIS:
        1. "summary": A concise 2-3 sentence summary of the meeting
        2. "keyPoints": An array of 3-5 key discussion points (strings)
        3. "decisions": An array of any decisions that were made (strings, can be empty if no decisions)

        ATTENDEE EXTRACTION:
        4. "extractedAttendees": An array of participant names identified from the transcript (strings).
           - Extract actual names when mentioned (e.g., "Sarah", "John Smith", "Dr. Williams")
           - If no names are mentioned, use role identifiers (e.g., "Project Manager", "Developer")
           - Do not include generic labels like "Speaker 1" unless no other identification is possible

        ACTION ITEMS:
        5. "actionItems": An array of action items identified in the meeting, each with:
           - "description": What needs to be done (string, required)
           - "assignee": Who is responsible, if mentioned (string or null)
           Example: [{"description": "Send updated budget proposal", "assignee": "Sarah"}, {"description": "Schedule follow-up meeting", "assignee": null}]

        BEHAVIORAL ANALYSIS:
        6. "behavioralAnalysis": An object containing behavioral insights (or null if insufficient data):
           a) "speakingDynamics": {
              "talkTimeByParticipant": [{"participant": "Name", "percentage": 35.5, "duration": "12:30"}],
              "interruptionPatterns": [{"interrupter": "Name", "interrupted": "Name", "count": 3}],
              "questionVsStatementRatio": {"Name": 0.4}
           }
           b) "sentimentTone": {
              "participantSentiments": [{"participant": "Name", "sentiment": "positive|neutral|negative", "score": 0.7}],
              "toneShifts": [{"timestamp": "10:30", "description": "Discussion became heated", "from": "collaborative", "to": "defensive"}],
              "emotionalIndicators": ["frustration detected in budget discussion"]
           }
           c) "communicationPatterns": {
              "overallClarity": 0.8,
              "followUpPatterns": [{"topic": "Q3 budget", "wasFollowedUp": true, "assignedTo": "Sarah"}],
              "engagementLevels": [{"participant": "Name", "level": "high|medium|low", "indicators": ["asked questions", "took notes"]}]
           }
           d) "redFlags": [
              {
                "type": "evasive|hedging|defensive|inconsistent",
                "participant": "Name",
                "description": "Avoided direct answer about timeline",
                "context": "When asked about delivery date...",
                "severity": "low|medium|high"
              }
           ]

        IMPORTANT GUIDELINES:
        - If participant names cannot be identified, use "Speaker 1", "Speaker 2", etc.
        - If the transcript lacks sufficient detail for behavioral analysis, set "behavioralAnalysis" to null
        - Red flags should only be included when there's clear evidence - avoid speculation
        - All percentages should sum to 100 for talk time
        - Sentiment scores range from 0.0 (most negative) to 1.0 (most positive)

        Respond ONLY with valid JSON, no other text or markdown formatting.

        Transcript:
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ClaudeMeetingAnalyzer(IOptions<MeetingAnalysisSettings> settings, ILogger<ClaudeMeetingAnalyzer> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Defer client creation until first use - allows app to start without API key
        // and provides a clear error message when analysis is attempted
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _client = new AnthropicClient(_settings.ApiKey);
        }
    }

    public async Task<MeetingAnalysisResult> AnalyzeAsync(string transcript, CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("Anthropic API key is not configured. Set MeetingAnalysis:ApiKey in appsettings or environment variables.");
        }

        // Use string concatenation to avoid format string vulnerabilities
        var prompt = AnalysisPrompt + transcript;

        var parameters = new MessageParameters
        {
            Model = _settings.Model,
            MaxTokens = _settings.MaxTokens,
            Messages = [new Message(RoleType.User, prompt)]
        };

        _logger.LogInformation("Starting meeting analysis with model {Model}", _settings.Model);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        var response = await _client.Messages.GetClaudeMessageAsync(parameters, cts.Token);

        var content = response.Content.OfType<TextContent>().FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Claude returned an empty response");
        }

        _logger.LogInformation("Received analysis response, parsing JSON");

        return ParseAnalysisResponse(content);
    }

    private static MeetingAnalysisResult ParseAnalysisResponse(string jsonResponse)
    {
        // Clean up the response - remove any markdown code blocks if present
        var cleanJson = jsonResponse.Trim();
        if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleanJson = cleanJson[7..];
        }
        else if (cleanJson.StartsWith("```"))
        {
            cleanJson = cleanJson[3..];
        }

        if (cleanJson.EndsWith("```"))
        {
            cleanJson = cleanJson[..^3];
        }

        cleanJson = cleanJson.Trim();

        var result = JsonSerializer.Deserialize<AnalysisJsonResponse>(cleanJson, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse analysis response");

        return new MeetingAnalysisResult(
            result.Summary ?? "No summary provided",
            result.KeyPoints ?? [],
            result.Decisions ?? [],
            MapBehavioralAnalysis(result.BehavioralAnalysis),
            result.ExtractedAttendees ?? [],
            result.ActionItems?
                .Where(a => !string.IsNullOrWhiteSpace(a.Description))
                .Select(a => new ExtractedActionItem(a.Description!.Trim(), a.Assignee?.Trim()))
                .ToList() ?? []);
    }

    private static BehavioralAnalysisData? MapBehavioralAnalysis(BehavioralAnalysisJson? json)
    {
        if (json is null)
            return null;

        return new BehavioralAnalysisData(
            SpeakingDynamics: new SpeakingDynamics(
                TalkTimeByParticipant: json.SpeakingDynamics?.TalkTimeByParticipant?
                    .Select(t => new ParticipantTalkTime(t.Participant ?? "Unknown", t.Percentage, t.Duration ?? "0:00"))
                    .ToList() ?? [],
                InterruptionPatterns: json.SpeakingDynamics?.InterruptionPatterns?
                    .Select(i => new InterruptionPattern(i.Interrupter ?? "Unknown", i.Interrupted ?? "Unknown", i.Count))
                    .ToList() ?? [],
                QuestionVsStatementRatio: json.SpeakingDynamics?.QuestionVsStatementRatio ?? new Dictionary<string, double>()
            ),
            SentimentTone: new SentimentTone(
                ParticipantSentiments: json.SentimentTone?.ParticipantSentiments?
                    .Select(s => new ParticipantSentiment(s.Participant ?? "Unknown", s.Sentiment ?? "neutral", s.Score))
                    .ToList() ?? [],
                ToneShifts: json.SentimentTone?.ToneShifts?
                    .Select(t => new ToneShift(t.Timestamp ?? "", t.Description ?? "", t.From ?? "", t.To ?? ""))
                    .ToList() ?? [],
                EmotionalIndicators: json.SentimentTone?.EmotionalIndicators ?? []
            ),
            CommunicationPatterns: new CommunicationPatterns(
                OverallClarity: json.CommunicationPatterns?.OverallClarity ?? 0.5,
                FollowUpPatterns: json.CommunicationPatterns?.FollowUpPatterns?
                    .Select(f => new FollowUpPattern(f.Topic ?? "", f.WasFollowedUp, f.AssignedTo))
                    .ToList() ?? [],
                EngagementLevels: json.CommunicationPatterns?.EngagementLevels?
                    .Select(e => new ParticipantEngagement(e.Participant ?? "Unknown", e.Level ?? "medium", e.Indicators ?? []))
                    .ToList() ?? []
            ),
            RedFlags: json.RedFlags?
                .Where(r => !string.IsNullOrWhiteSpace(r.Type) && !string.IsNullOrWhiteSpace(r.Severity))
                .Select(r => new RedFlag(r.Type!, r.Participant ?? "Unknown", r.Description ?? "", r.Context ?? "", r.Severity!))
                .ToList() ?? []
        );
    }

    #region JSON Response Classes

    private sealed class AnalysisJsonResponse
    {
        public string? Summary { get; set; }
        public List<string>? KeyPoints { get; set; }
        public List<string>? Decisions { get; set; }
        public List<string>? ExtractedAttendees { get; set; }
        public List<ActionItemJson>? ActionItems { get; set; }
        public BehavioralAnalysisJson? BehavioralAnalysis { get; set; }
    }

    private sealed class ActionItemJson
    {
        public string? Description { get; set; }
        public string? Assignee { get; set; }
    }

    private sealed class BehavioralAnalysisJson
    {
        public SpeakingDynamicsJson? SpeakingDynamics { get; set; }
        public SentimentToneJson? SentimentTone { get; set; }
        public CommunicationPatternsJson? CommunicationPatterns { get; set; }
        public List<RedFlagJson>? RedFlags { get; set; }
    }

    private sealed class SpeakingDynamicsJson
    {
        public List<ParticipantTalkTimeJson>? TalkTimeByParticipant { get; set; }
        public List<InterruptionPatternJson>? InterruptionPatterns { get; set; }
        public Dictionary<string, double>? QuestionVsStatementRatio { get; set; }
    }

    private sealed class ParticipantTalkTimeJson
    {
        public string? Participant { get; set; }
        public double Percentage { get; set; }
        public string? Duration { get; set; }
    }

    private sealed class InterruptionPatternJson
    {
        public string? Interrupter { get; set; }
        public string? Interrupted { get; set; }
        public int Count { get; set; }
    }

    private sealed class SentimentToneJson
    {
        public List<ParticipantSentimentJson>? ParticipantSentiments { get; set; }
        public List<ToneShiftJson>? ToneShifts { get; set; }
        public List<string>? EmotionalIndicators { get; set; }
    }

    private sealed class ParticipantSentimentJson
    {
        public string? Participant { get; set; }
        public string? Sentiment { get; set; }
        public double Score { get; set; }
    }

    private sealed class ToneShiftJson
    {
        public string? Timestamp { get; set; }
        public string? Description { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
    }

    private sealed class CommunicationPatternsJson
    {
        public double OverallClarity { get; set; }
        public List<FollowUpPatternJson>? FollowUpPatterns { get; set; }
        public List<ParticipantEngagementJson>? EngagementLevels { get; set; }
    }

    private sealed class FollowUpPatternJson
    {
        public string? Topic { get; set; }
        public bool WasFollowedUp { get; set; }
        public string? AssignedTo { get; set; }
    }

    private sealed class ParticipantEngagementJson
    {
        public string? Participant { get; set; }
        public string? Level { get; set; }
        public List<string>? Indicators { get; set; }
    }

    private sealed class RedFlagJson
    {
        public string? Type { get; set; }
        public string? Participant { get; set; }
        public string? Description { get; set; }
        public string? Context { get; set; }
        public string? Severity { get; set; }
    }

    #endregion
}
