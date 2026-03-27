using System.Text.Json;

namespace PraxisNote.Infrastructure.External;

/// <summary>
/// Shared JSON serializer options and DTO models for Gemini API communication.
/// Used by both GeminiMeetingAnalyzer and GeminiTagAiChatService.
/// </summary>
internal static class GeminiJsonConfiguration
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    internal sealed class GeminiRequest
    {
        public List<GeminiContent> Contents { get; set; } = [];
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    internal sealed class GeminiContent
    {
        public string? Role { get; set; }
        public List<GeminiPart> Parts { get; set; } = [];
    }

    internal sealed class GeminiPart
    {
        public string? Text { get; set; }
        public GeminiInlineData? InlineData { get; set; }
    }

    internal sealed class GeminiInlineData
    {
        public string MimeType { get; set; } = "";
        public string Data { get; set; } = "";
    }

    internal sealed class GeminiGenerationConfig
    {
        public int MaxOutputTokens { get; set; }
    }

    internal sealed class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    internal sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    internal sealed class GeminiStreamRequest
    {
        public GeminiContent? SystemInstruction { get; set; }
        public List<GeminiContent> Contents { get; set; } = [];
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }
}
