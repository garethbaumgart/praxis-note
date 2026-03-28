namespace PraxisNote.Application.Features.UserAiKeys;

public sealed class AiKeyInvalidException : Exception
{
    public string Provider { get; }

    public AiKeyInvalidException(string provider)
        : base($"API key rejected by {provider}.")
    {
        Provider = provider;
    }
}

public sealed class AiRateLimitedException : Exception
{
    public string Provider { get; }
    public int? RetryAfterSeconds { get; }

    public AiRateLimitedException(string provider, int? retryAfterSeconds = null)
        : base($"Rate limit reached with {provider}.")
    {
        Provider = provider;
        RetryAfterSeconds = retryAfterSeconds;
    }
}

public sealed class AiProviderException : Exception
{
    public string Provider { get; }

    public AiProviderException(string provider, string message, Exception? inner = null)
        : base(message, inner)
    {
        Provider = provider;
    }
}
