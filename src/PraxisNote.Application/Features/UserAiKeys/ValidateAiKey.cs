using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Application.Features.UserAiKeys;

public sealed class ValidateAiKey(
    IAiProviderFactory providerFactory,
    IOptions<AiProviderSettings> settings,
    ILogger<ValidateAiKey> logger)
{
    private readonly AiProviderSettings _settings = settings.Value;

    public record Command(AiProvider Provider, string ApiKey);

    public record Result(bool Validated, bool RateLimited = false);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        try
        {
            var model = command.Provider switch
            {
                AiProvider.Anthropic => _settings.Anthropic.DefaultModel,
                AiProvider.OpenAI => _settings.OpenAI.DefaultModel,
                AiProvider.Gemini => _settings.Gemini.DefaultModel,
                _ => throw new ArgumentOutOfRangeException(nameof(command.Provider))
            };

            var chatService = providerFactory.CreateTagAiChatService(command.ApiKey, command.Provider, model);

            // Use a minimal chat call — cheaper than full meeting analysis
            var minimalContext = new TagChatContext("test", [], [], []);
            await foreach (var _ in chatService.StreamResponseAsync(minimalContext, "Say hi", [], cancellationToken))
            {
                // First token received means the key is valid — stop immediately
                break;
            }

            return new Result(true);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized
                                               or HttpStatusCode.Forbidden
                                               or HttpStatusCode.BadRequest)
        {
            logger.LogInformation("AI key validation failed for provider {Provider}: {Status} {Message}",
                command.Provider, ex.StatusCode, ex.Message);
            return new Result(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            logger.LogInformation("AI key validation rate-limited for provider {Provider}", command.Provider);
            return new Result(true, RateLimited: true);
        }
        catch (HttpRequestException ex)
        {
            // Network-level error (DNS, timeout, 5xx) — inconclusive, key may be valid
            logger.LogWarning(ex, "AI key validation inconclusive (network) for provider {Provider}", command.Provider);
            return new Result(true, RateLimited: true);
        }
        catch (Exception ex)
        {
            // Unexpected error (serialization, SDK issue) — treat as invalid to be safe
            logger.LogWarning(ex, "AI key validation failed unexpectedly for provider {Provider}", command.Provider);
            return new Result(false);
        }
    }
}
