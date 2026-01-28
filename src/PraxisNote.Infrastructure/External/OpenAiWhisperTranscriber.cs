using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class OpenAiWhisperTranscriber : ITranscriptionService
{
    private const string WhisperEndpoint = "https://api.openai.com/v1/audio/transcriptions";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly WhisperTranscriptionSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiWhisperTranscriber> _logger;

    public OpenAiWhisperTranscriber(
        IOptions<WhisperTranscriptionSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAiWhisperTranscriber> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Set WhisperTranscription:ApiKey in appsettings or environment variables.");
        }

        _logger.LogInformation("Starting Whisper transcription for file {FileName}", fileName);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(audioStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
        content.Add(streamContent, "file", fileName);
        content.Add(new StringContent(_settings.Model), "model");
        content.Add(new StringContent("json"), "response_format");

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, WhisperEndpoint)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var response = await client.SendAsync(request, cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
            _logger.LogError("Whisper API returned {StatusCode}: {Body}", response.StatusCode, errorBody);
            throw new InvalidOperationException(
                $"Whisper API returned {(int)response.StatusCode}: {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(cts.Token);
        var result = JsonSerializer.Deserialize<WhisperResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse Whisper API response");

        if (string.IsNullOrWhiteSpace(result.Text))
        {
            throw new InvalidOperationException("Whisper API returned empty transcription");
        }

        _logger.LogInformation(
            "Transcription complete for {FileName}, language: {Language}",
            fileName,
            result.Language ?? "unknown");

        return new TranscriptionResult(result.Text, result.Language);
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp3" => "audio/mpeg",
            ".mp4" => "audio/mp4",
            ".mpeg" => "audio/mpeg",
            ".mpga" => "audio/mpeg",
            ".m4a" => "audio/mp4",
            ".wav" => "audio/wav",
            ".webm" => "audio/webm",
            _ => "application/octet-stream"
        };
    }

    private sealed class WhisperResponse
    {
        public string? Text { get; set; }
        public string? Language { get; set; }
    }
}
