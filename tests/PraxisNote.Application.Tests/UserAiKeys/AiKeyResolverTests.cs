using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.UserAiKeys.Services;
using PraxisNote.Domain.Aggregates.UserAiKeys;
using PraxisNote.Infrastructure.External;

namespace PraxisNote.Application.Tests.UserAiKeys;

public class AiKeyResolverTests
{
    private readonly IUserAiKeyRepository _repo = Substitute.For<IUserAiKeyRepository>();
    private readonly IAiKeyEncryptionService _encryption = Substitute.For<IAiKeyEncryptionService>();
    private readonly ILogger<AiKeyResolver> _logger = Substitute.For<ILogger<AiKeyResolver>>();
    private readonly Guid _userId = Guid.NewGuid();

    private AiKeyResolver CreateSut(AiProviderSettings? settings = null)
    {
        settings ??= new AiProviderSettings();
        return new AiKeyResolver(_repo, _encryption, Options.Create(settings), _logger);
    }

    #region User key present

    [Fact]
    public async Task ResolveAsync_UserKeyPresent_ReturnsUserKey()
    {
        var userKey = UserAiKey.Create(_userId, AiProvider.Anthropic, "enc_key", "****", "claude-4");
        _repo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([userKey]);
        _encryption.Decrypt("enc_key").Returns("sk-real-key");

        var sut = CreateSut();
        var result = await sut.ResolveAsync(_userId);

        Assert.NotNull(result);
        Assert.Equal(AiProvider.Anthropic, result.Provider);
        Assert.Equal("sk-real-key", result.ApiKey);
        Assert.Equal("claude-4", result.Model);
    }

    [Fact]
    public async Task ResolveAsync_UserKeyWithNoPreferredModel_UsesDefaultModel()
    {
        var userKey = UserAiKey.Create(_userId, AiProvider.OpenAI, "enc_key", "****", null);
        _repo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([userKey]);
        _encryption.Decrypt("enc_key").Returns("sk-openai-key");

        var settings = new AiProviderSettings
        {
            OpenAI = new OpenAiProviderConfig { DefaultModel = "gpt-4o" }
        };
        var sut = CreateSut(settings);
        var result = await sut.ResolveAsync(_userId);

        Assert.NotNull(result);
        Assert.Equal(AiProvider.OpenAI, result.Provider);
        Assert.Equal("gpt-4o", result.Model);
    }

    [Fact]
    public async Task ResolveAsync_MultipleUserKeys_PrefersAnthropic()
    {
        var anthropicKey = UserAiKey.Create(_userId, AiProvider.Anthropic, "enc_anthropic", "****", "claude-4");
        var openAiKey = UserAiKey.Create(_userId, AiProvider.OpenAI, "enc_openai", "****", "gpt-4o");
        _repo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([openAiKey, anthropicKey]);
        _encryption.Decrypt("enc_anthropic").Returns("sk-anthropic");

        var sut = CreateSut();
        var result = await sut.ResolveAsync(_userId);

        Assert.NotNull(result);
        Assert.Equal(AiProvider.Anthropic, result.Provider);
        Assert.Equal("sk-anthropic", result.ApiKey);
    }

    #endregion

    #region No user key, app default present

    [Fact]
    public async Task ResolveAsync_NoUserKey_AppDefaultAnthropicPresent_ReturnsAppDefault()
    {
        _repo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([]);

        var settings = new AiProviderSettings
        {
            Anthropic = new AnthropicProviderConfig { ApiKey = "app-anthropic-key", DefaultModel = "claude-sonnet-4-6" }
        };
        var sut = CreateSut(settings);
        var result = await sut.ResolveAsync(_userId);

        Assert.NotNull(result);
        Assert.Equal(AiProvider.Anthropic, result.Provider);
        Assert.Equal("app-anthropic-key", result.ApiKey);
        Assert.Equal("claude-sonnet-4-6", result.Model);
    }

    [Fact]
    public async Task ResolveAsync_NoUserKey_NoAnthropicDefault_FallsBackToOpenAi()
    {
        _repo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([]);

        var settings = new AiProviderSettings
        {
            Anthropic = new AnthropicProviderConfig { ApiKey = null },
            OpenAI = new OpenAiProviderConfig { ApiKey = "app-openai-key", DefaultModel = "gpt-4o-mini" }
        };
        var sut = CreateSut(settings);
        var result = await sut.ResolveAsync(_userId);

        Assert.NotNull(result);
        Assert.Equal(AiProvider.OpenAI, result.Provider);
        Assert.Equal("app-openai-key", result.ApiKey);
        Assert.Equal("gpt-4o-mini", result.Model);
    }

    #endregion

    #region Gemini free tier fallback

    [Fact]
    public async Task ResolveAsync_NoUserKey_NoAppDefault_GeminiFreeKey_ReturnsGemini()
    {
        _repo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([]);

        var settings = new AiProviderSettings
        {
            Anthropic = new AnthropicProviderConfig { ApiKey = null },
            OpenAI = new OpenAiProviderConfig { ApiKey = null },
            Gemini = new GeminiProviderConfig { ApiKey = "gemini-free-key", DefaultModel = "gemini-1.5-flash" }
        };
        var sut = CreateSut(settings);
        var result = await sut.ResolveAsync(_userId);

        Assert.NotNull(result);
        Assert.Equal(AiProvider.Gemini, result.Provider);
        Assert.Equal("gemini-free-key", result.ApiKey);
        Assert.Equal("gemini-1.5-flash", result.Model);
    }

    #endregion

    #region No keys at all

    [Fact]
    public async Task ResolveAsync_NoKeysAtAll_ReturnsNull()
    {
        _repo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns([]);

        var settings = new AiProviderSettings
        {
            Anthropic = new AnthropicProviderConfig { ApiKey = null },
            OpenAI = new OpenAiProviderConfig { ApiKey = null },
            Gemini = new GeminiProviderConfig { ApiKey = null }
        };
        var sut = CreateSut(settings);
        var result = await sut.ResolveAsync(_userId);

        Assert.Null(result);
    }

    #endregion
}
