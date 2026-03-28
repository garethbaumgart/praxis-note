using PraxisNote.Application.Features.UserAiKeys;

namespace PraxisNote.Application.Tests.UserAiKeys;

public class AiProviderExceptionTests
{
    #region AiKeyInvalidException

    [Fact]
    public void AiKeyInvalidException_SetsProvider()
    {
        var ex = new AiKeyInvalidException("Gemini");

        Assert.Equal("Gemini", ex.Provider);
        Assert.Contains("Gemini", ex.Message);
    }

    [Theory]
    [InlineData("Anthropic")]
    [InlineData("OpenAI")]
    [InlineData("Gemini")]
    public void AiKeyInvalidException_MessageContainsProvider(string provider)
    {
        var ex = new AiKeyInvalidException(provider);

        Assert.Contains(provider, ex.Message);
    }

    #endregion

    #region AiRateLimitedException

    [Fact]
    public void AiRateLimitedException_SetsProviderAndRetryAfter()
    {
        var ex = new AiRateLimitedException("OpenAI", 60);

        Assert.Equal("OpenAI", ex.Provider);
        Assert.Equal(60, ex.RetryAfterSeconds);
        Assert.Contains("OpenAI", ex.Message);
    }

    [Fact]
    public void AiRateLimitedException_RetryAfterDefaultsToNull()
    {
        var ex = new AiRateLimitedException("Anthropic");

        Assert.Null(ex.RetryAfterSeconds);
    }

    #endregion

    #region AiProviderException

    [Fact]
    public void AiProviderException_SetsProviderAndMessage()
    {
        var ex = new AiProviderException("Gemini", "Gemini is not responding. Try again shortly.");

        Assert.Equal("Gemini", ex.Provider);
        Assert.Equal("Gemini is not responding. Try again shortly.", ex.Message);
    }

    [Fact]
    public void AiProviderException_WrapsInnerException()
    {
        var inner = new TimeoutException("Connection timed out");
        var ex = new AiProviderException("OpenAI", "OpenAI is not responding.", inner);

        Assert.Equal("OpenAI", ex.Provider);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AiProviderException_InnerDefaultsToNull()
    {
        var ex = new AiProviderException("Anthropic", "Error occurred");

        Assert.Null(ex.InnerException);
    }

    #endregion
}
