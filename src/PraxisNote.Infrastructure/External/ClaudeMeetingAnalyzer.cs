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
    private AnthropicClient? _client;

    private const string AnalysisPrompt = """
        Analyze this meeting transcript and provide a JSON response with:
        1. "summary": A concise 2-3 sentence summary of the meeting
        2. "keyPoints": An array of 3-5 key discussion points (strings)
        3. "decisions": An array of any decisions that were made (strings, can be empty if no decisions)

        Respond ONLY with valid JSON, no other text or markdown formatting.

        Transcript:
        """;

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

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var result = JsonSerializer.Deserialize<AnalysisJsonResponse>(cleanJson, options)
            ?? throw new InvalidOperationException("Failed to parse analysis response");

        return new MeetingAnalysisResult(
            result.Summary ?? "No summary provided",
            result.KeyPoints ?? [],
            result.Decisions ?? []);
    }

    private sealed class AnalysisJsonResponse
    {
        public string? Summary { get; set; }
        public List<string>? KeyPoints { get; set; }
        public List<string>? Decisions { get; set; }
    }
}
