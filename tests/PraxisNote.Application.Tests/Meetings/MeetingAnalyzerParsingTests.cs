using System.Text.Json;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Infrastructure.External;

namespace PraxisNote.Application.Tests.Meetings;

public class MeetingAnalyzerParsingTests
{
    #region ParseAnalysisResponse

    [Fact]
    public void ParseAnalysisResponse_ValidJson_ReturnsMeetingAnalysisResult()
    {
        var json = """
            {
              "summary": "Discussed Q3 budget and hiring plan.",
              "keyPoints": ["Budget approved", "Hiring 3 engineers"],
              "decisions": ["Proceed with hiring"],
              "extractedAttendees": ["Alice", "Bob"],
              "actionItems": [
                {"description": "Draft job postings", "assignee": "Alice"},
                {"description": "Update budget spreadsheet", "assignee": null}
              ],
              "suggestedTitle": "Q3 Budget Review with Alice & Bob",
              "suggestedTags": ["budget", "hiring"],
              "behavioralAnalysis": null
            }
            """;

        var result = AnthropicMeetingAnalyzer.ParseAnalysisResponse(json);

        Assert.Equal("Discussed Q3 budget and hiring plan.", result.Summary);
        Assert.Equal(2, result.KeyPoints.Count);
        Assert.Single(result.Decisions);
        Assert.Equal(2, result.ExtractedAttendees.Count);
        Assert.Equal(2, result.ExtractedActionItems.Count);
        Assert.Equal("Draft job postings", result.ExtractedActionItems[0].Description);
        Assert.Equal("Alice", result.ExtractedActionItems[0].Assignee);
        Assert.Null(result.ExtractedActionItems[1].Assignee);
        Assert.Equal("Q3 Budget Review with Alice & Bob", result.SuggestedTitle);
        Assert.Equal(2, result.SuggestedTags.Count);
    }

    [Fact]
    public void ParseAnalysisResponse_WithMarkdownCodeBlock_StripsWrapper()
    {
        var json = """
            ```json
            {
              "summary": "Meeting summary.",
              "keyPoints": [],
              "decisions": [],
              "extractedAttendees": [],
              "actionItems": [],
              "suggestedTitle": null,
              "suggestedTags": [],
              "behavioralAnalysis": null
            }
            ```
            """;

        var result = AnthropicMeetingAnalyzer.ParseAnalysisResponse(json);

        Assert.Equal("Meeting summary.", result.Summary);
    }

    [Fact]
    public void ParseAnalysisResponse_NullOptionalFields_UsesDefaults()
    {
        var json = """
            {
              "summary": "Summary only.",
              "keyPoints": null,
              "decisions": null,
              "extractedAttendees": null,
              "actionItems": null,
              "suggestedTitle": null,
              "suggestedTags": null,
              "behavioralAnalysis": null
            }
            """;

        var result = AnthropicMeetingAnalyzer.ParseAnalysisResponse(json);

        Assert.Equal("Summary only.", result.Summary);
        Assert.Empty(result.KeyPoints);
        Assert.Empty(result.Decisions);
        Assert.Empty(result.ExtractedAttendees);
        Assert.Empty(result.ExtractedActionItems);
        Assert.Null(result.SuggestedTitle);
        Assert.Empty(result.SuggestedTags);
    }

    #endregion

    #region ParseScreenshotExtractionResponse

    [Fact]
    public void ParseScreenshotExtractionResponse_ValidJson_ReturnsEvents()
    {
        var json = """
            {
              "events": [
                {
                  "title": "Team Standup",
                  "startTime": "2025-01-15T09:00:00+02:00",
                  "endTime": "2025-01-15T09:30:00+02:00",
                  "attendees": "Alice, Bob",
                  "location": "Room 42"
                },
                {
                  "title": "1:1 with Sarah",
                  "startTime": "2025-01-15T10:00:00+02:00",
                  "endTime": "2025-01-15T10:30:00+02:00",
                  "attendees": null,
                  "location": null
                }
              ]
            }
            """;

        var result = AnthropicMeetingAnalyzer.ParseScreenshotExtractionResponse(json);

        Assert.Equal(2, result.Events.Count);
        Assert.Equal("Team Standup", result.Events[0].Title);
        Assert.Equal("Alice, Bob", result.Events[0].Attendees);
        Assert.Equal("Room 42", result.Events[0].Location);
        Assert.Null(result.Events[1].Attendees);
    }

    [Fact]
    public void ParseScreenshotExtractionResponse_FiltersInvalidEvents()
    {
        var json = """
            {
              "events": [
                {
                  "title": "Valid Event",
                  "startTime": "2025-01-15T09:00:00+02:00",
                  "endTime": "2025-01-15T09:30:00+02:00",
                  "attendees": null,
                  "location": null
                },
                {
                  "title": "",
                  "startTime": "2025-01-15T10:00:00+02:00",
                  "endTime": "2025-01-15T10:30:00+02:00",
                  "attendees": null,
                  "location": null
                },
                {
                  "title": "End Before Start",
                  "startTime": "2025-01-15T10:00:00+02:00",
                  "endTime": "2025-01-15T09:00:00+02:00",
                  "attendees": null,
                  "location": null
                }
              ]
            }
            """;

        var result = AnthropicMeetingAnalyzer.ParseScreenshotExtractionResponse(json);

        Assert.Single(result.Events);
        Assert.Equal("Valid Event", result.Events[0].Title);
    }

    #endregion

    #region ParseTranscriptImportResponse

    [Fact]
    public void ParseTranscriptImportResponse_CompleteJson_ReturnsCompleteResult()
    {
        var json = """
            {
              "title": "Sprint Planning with Team",
              "meetingDate": "2025-06-15T09:00:00+02:00",
              "attendees": "Alice Smith, Bob Jones",
              "summary": "Discussed sprint goals and assigned tickets.",
              "keyPoints": ["Sprint goal set", "Velocity reviewed"],
              "decisions": ["Focus on auth module"],
              "actionItems": [
                {"description": "Create auth tickets", "assignee": "Alice"}
              ],
              "suggestedTags": ["sprint", "planning"],
              "isComplete": true,
              "warning": null,
              "isAdhoc": false
            }
            """;

        var result = AnthropicMeetingAnalyzer.ParseTranscriptImportResponse(json);

        Assert.Equal("Sprint Planning with Team", result.Title);
        Assert.NotNull(result.MeetingDate);
        Assert.Equal("Alice Smith, Bob Jones", result.Attendees);
        Assert.True(result.IsComplete);
        Assert.Null(result.Warning);
        Assert.False(result.IsAdhoc);
        Assert.Single(result.ActionItems);
    }

    [Fact]
    public void ParseTranscriptImportResponse_MissingDate_MarksIncomplete()
    {
        var json = """
            {
              "title": "Quick Chat",
              "meetingDate": null,
              "attendees": null,
              "summary": "Informal discussion.",
              "keyPoints": [],
              "decisions": [],
              "actionItems": [],
              "suggestedTags": [],
              "isComplete": false,
              "warning": "Date could not be determined",
              "isAdhoc": true
            }
            """;

        var result = AnthropicMeetingAnalyzer.ParseTranscriptImportResponse(json);

        Assert.False(result.IsComplete);
        Assert.Null(result.MeetingDate);
        Assert.True(result.IsAdhoc);
        Assert.NotNull(result.Warning);
    }

    [Fact]
    public void ParseTranscriptImportResponse_InvalidDate_FallsBackGracefully()
    {
        var json = """
            {
              "title": "Meeting",
              "meetingDate": "not-a-date",
              "attendees": null,
              "summary": "Summary text.",
              "keyPoints": [],
              "decisions": [],
              "actionItems": [],
              "suggestedTags": [],
              "isComplete": true,
              "warning": null,
              "isAdhoc": false
            }
            """;

        var result = AnthropicMeetingAnalyzer.ParseTranscriptImportResponse(json);

        Assert.Null(result.MeetingDate);
        Assert.False(result.IsComplete);
        Assert.Contains("meeting date", result.Warning);
    }

    #endregion

    #region CleanJsonResponse

    [Fact]
    public void CleanJsonResponse_PlainJson_ReturnsUnchanged()
    {
        var json = """{"key": "value"}""";
        Assert.Equal(json, AnthropicMeetingAnalyzer.CleanJsonResponse(json));
    }

    [Fact]
    public void CleanJsonResponse_JsonCodeBlock_StripsMarkers()
    {
        var input = "```json\n{\"key\": \"value\"}\n```";
        Assert.Equal("{\"key\": \"value\"}", AnthropicMeetingAnalyzer.CleanJsonResponse(input));
    }

    [Fact]
    public void CleanJsonResponse_GenericCodeBlock_StripsMarkers()
    {
        var input = "```\n{\"key\": \"value\"}\n```";
        Assert.Equal("{\"key\": \"value\"}", AnthropicMeetingAnalyzer.CleanJsonResponse(input));
    }

    #endregion

    #region Gemini JSON Models

    [Fact]
    public void GeminiRequest_SerializesCorrectly()
    {
        var request = new GeminiJsonConfiguration.GeminiRequest
        {
            Contents =
            [
                new GeminiJsonConfiguration.GeminiContent
                {
                    Parts = [new GeminiJsonConfiguration.GeminiPart { Text = "Analyze this" }]
                }
            ],
            GenerationConfig = new GeminiJsonConfiguration.GeminiGenerationConfig { MaxOutputTokens = 4096 }
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.Contains("\"contents\"", json);
        Assert.Contains("\"Analyze this\"", json);
        Assert.Contains("\"maxOutputTokens\":4096", json);
    }

    [Fact]
    public void GeminiResponse_DeserializesTextContent()
    {
        var json = """
            {
              "candidates": [
                {
                  "content": {
                    "parts": [
                      { "text": "{\"summary\": \"Test meeting\"}" }
                    ]
                  }
                }
              ]
            }
            """;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        var response = JsonSerializer.Deserialize<GeminiJsonConfiguration.GeminiResponse>(json, options);

        Assert.NotNull(response?.Candidates);
        Assert.Single(response.Candidates);
        var text = response.Candidates[0].Content?.Parts?.FirstOrDefault()?.Text;
        Assert.Contains("Test meeting", text);
    }

    [Fact]
    public void GeminiRequest_WithInlineImage_SerializesCorrectly()
    {
        var request = new GeminiJsonConfiguration.GeminiRequest
        {
            Contents =
            [
                new GeminiJsonConfiguration.GeminiContent
                {
                    Parts =
                    [
                        new GeminiJsonConfiguration.GeminiPart
                        {
                            InlineData = new GeminiJsonConfiguration.GeminiInlineData
                            {
                                MimeType = "image/png",
                                Data = "base64data"
                            }
                        },
                        new GeminiJsonConfiguration.GeminiPart { Text = "Extract events" }
                    ]
                }
            ]
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.Contains("\"mimeType\":\"image/png\"", json);
        Assert.Contains("\"data\":\"base64data\"", json);
        Assert.Contains("\"Extract events\"", json);
    }

    #endregion

    #region AiProviderFactory

    [Fact]
    public void AiProviderFactory_CreateMeetingAnalyzer_ReturnsCorrectType()
    {
        var factory = CreateFactory();

        var anthropic = factory.CreateMeetingAnalyzer("test-key", Domain.Aggregates.UserAiKeys.AiProvider.Anthropic, "claude-sonnet-4-6");
        var gemini = factory.CreateMeetingAnalyzer("test-key", Domain.Aggregates.UserAiKeys.AiProvider.Gemini, "gemini-1.5-flash");
        var openai = factory.CreateMeetingAnalyzer("test-key", Domain.Aggregates.UserAiKeys.AiProvider.OpenAI, "gpt-4o-mini");

        Assert.IsType<AnthropicMeetingAnalyzer>(anthropic);
        Assert.IsType<GeminiMeetingAnalyzer>(gemini);
        Assert.IsType<OpenAiMeetingAnalyzer>(openai);
    }

    [Fact]
    public void AiProviderFactory_CreateTagAiChatService_ReturnsCorrectType()
    {
        var factory = CreateFactory();

        var anthropic = factory.CreateTagAiChatService("test-key", Domain.Aggregates.UserAiKeys.AiProvider.Anthropic, "claude-sonnet-4-6");
        var gemini = factory.CreateTagAiChatService("test-key", Domain.Aggregates.UserAiKeys.AiProvider.Gemini, "gemini-1.5-flash");
        var openai = factory.CreateTagAiChatService("test-key", Domain.Aggregates.UserAiKeys.AiProvider.OpenAI, "gpt-4o-mini");

        Assert.IsType<AnthropicTagAiChatService>(anthropic);
        Assert.IsType<GeminiTagAiChatService>(gemini);
        Assert.IsType<OpenAiTagAiChatService>(openai);
    }

    [Fact]
    public void AiProviderFactory_InvalidProvider_ThrowsArgumentOutOfRange()
    {
        var factory = CreateFactory();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            factory.CreateMeetingAnalyzer("key", (Domain.Aggregates.UserAiKeys.AiProvider)99, "model"));
    }

    [Fact]
    public void AiProviderFactory_InvalidProvider_TagAiChatService_ThrowsArgumentOutOfRange()
    {
        var factory = CreateFactory();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            factory.CreateTagAiChatService("key", (Domain.Aggregates.UserAiKeys.AiProvider)99, "model"));
    }

    private static AiProviderFactory CreateFactory()
    {
        var settings = Microsoft.Extensions.Options.Options.Create(new AiProviderSettings());
        var httpClientFactory = new SimpleHttpClientFactory();
        var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        return new AiProviderFactory(settings, httpClientFactory, loggerFactory);
    }

    private sealed class SimpleHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    #endregion
}
