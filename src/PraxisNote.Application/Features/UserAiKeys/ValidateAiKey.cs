using Microsoft.Extensions.Logging;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Application.Features.UserAiKeys;

public sealed class ValidateAiKey(
    IAiProviderFactory providerFactory,
    ILogger<ValidateAiKey> logger)
{
    public record Command(AiProvider Provider, string ApiKey);

    public record Result(bool Validated, bool RateLimited = false);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        try
        {
            var model = command.Provider switch
            {
                AiProvider.Anthropic => "claude-sonnet-4-6",
                AiProvider.OpenAI => "gpt-4o-mini",
                AiProvider.Gemini => "gemini-1.5-flash",
                _ => throw new ArgumentOutOfRangeException(nameof(command.Provider))
            };

            var analyzer = providerFactory.CreateMeetingAnalyzer(command.ApiKey, command.Provider, model);

            // Attempt a minimal call — AnalyzeAsync with a trivial transcript
            // This will fail fast if the key is invalid
            await analyzer.AnalyzeAsync("test", cancellationToken);

            return new Result(true);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized
                                               || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            logger.LogInformation("AI key validation failed for provider {Provider}: {Message}", command.Provider, ex.Message);
            return new Result(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            logger.LogInformation("AI key validation rate-limited for provider {Provider}", command.Provider);
            return new Result(true, RateLimited: true);
        }
        catch (Exception ex)
        {
            // Key might be valid but the test call failed for other reasons (network, model error, etc.)
            // Treat as rate-limited/inconclusive — the key was saved, validation is best-effort
            logger.LogWarning(ex, "AI key validation inconclusive for provider {Provider}", command.Provider);
            return new Result(true, RateLimited: true);
        }
    }
}
