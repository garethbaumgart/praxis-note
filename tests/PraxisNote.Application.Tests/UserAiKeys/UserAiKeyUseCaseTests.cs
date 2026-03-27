using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.UserAiKeys;
using PraxisNote.Application.Features.UserAiKeys.Services;
using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Application.Tests.UserAiKeys;

public class UserAiKeyUseCaseTests
{
    private readonly IUserAiKeyRepository _repo = Substitute.For<IUserAiKeyRepository>();
    private readonly IAiKeyEncryptionService _encryption = Substitute.For<IAiKeyEncryptionService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Guid _userId = Guid.NewGuid();

    public UserAiKeyUseCaseTests()
    {
        _encryption.Encrypt(Arg.Any<string>()).Returns(c => $"enc_{c.Arg<string>()}");
        _encryption.ComputeHint(Arg.Any<string>()).Returns("****...abcd");
    }

    #region UpsertUserAiKey

    [Fact]
    public async Task Upsert_WithValidKey_CallsRepositoryUpsertAndSaves()
    {
        var sut = new UpsertUserAiKey(_repo, _encryption, _unitOfWork);
        var command = new UpsertUserAiKey.Command(_userId, AiProvider.Anthropic, "sk-test-key", "claude-4");

        var key = UserAiKey.Create(_userId, AiProvider.Anthropic, "enc_sk-test-key", "****...abcd", "claude-4");
        _repo.UpsertAsync(_userId, AiProvider.Anthropic, "enc_sk-test-key", "****...abcd", "claude-4", Arg.Any<CancellationToken>())
            .Returns(key);

        await sut.ExecuteAsync(command);

        _encryption.Received(1).Encrypt("sk-test-key");
        _encryption.Received(1).ComputeHint("sk-test-key");
        await _repo.Received(1).UpsertAsync(_userId, AiProvider.Anthropic, "enc_sk-test-key", "****...abcd", "claude-4", Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upsert_WithNullModel_PassesNullToRepository()
    {
        var sut = new UpsertUserAiKey(_repo, _encryption, _unitOfWork);
        var command = new UpsertUserAiKey.Command(_userId, AiProvider.OpenAI, "sk-key", null);

        var key = UserAiKey.Create(_userId, AiProvider.OpenAI, "enc_sk-key", "****...abcd", null);
        _repo.UpsertAsync(_userId, AiProvider.OpenAI, "enc_sk-key", "****...abcd", null, Arg.Any<CancellationToken>())
            .Returns(key);

        await sut.ExecuteAsync(command);

        await _repo.Received(1).UpsertAsync(_userId, AiProvider.OpenAI, "enc_sk-key", "****...abcd", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upsert_WithNullApiKey_ThrowsArgumentNullException()
    {
        var sut = new UpsertUserAiKey(_repo, _encryption, _unitOfWork);
        var command = new UpsertUserAiKey.Command(_userId, AiProvider.Anthropic, null!, null);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.ExecuteAsync(command));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Upsert_WithEmptyOrWhitespaceApiKey_ThrowsArgumentException(string apiKey)
    {
        var sut = new UpsertUserAiKey(_repo, _encryption, _unitOfWork);
        var command = new UpsertUserAiKey.Command(_userId, AiProvider.Anthropic, apiKey, null);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task Upsert_WithWhitespaceApiKey_DoesNotCallRepository()
    {
        var sut = new UpsertUserAiKey(_repo, _encryption, _unitOfWork);
        var command = new UpsertUserAiKey.Command(_userId, AiProvider.Anthropic, "   ", null);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.ExecuteAsync(command));

        await _repo.DidNotReceiveWithAnyArgs().UpsertAsync(default, default, default!, default!, default, default);
    }

    #endregion

    #region GetUserAiKeys

    [Fact]
    public async Task Get_ReturnsProjectedDtos()
    {
        var sut = new GetUserAiKeys(_repo);
        var key = UserAiKey.Create(_userId, AiProvider.Gemini, "enc", "****...hint", "gemini-pro");
        _repo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new List<UserAiKey> { key });

        var result = await sut.ExecuteAsync(new GetUserAiKeys.Query(_userId));

        Assert.Single(result);
        var dto = result[0];
        Assert.Equal("Gemini", dto.Provider);
        Assert.True(dto.HasKey);
        Assert.Equal("****...hint", dto.KeyHint);
        Assert.Equal("gemini-pro", dto.PreferredModel);
        Assert.NotNull(dto.CreatedAt);
    }

    [Fact]
    public async Task Get_WithNoKeys_ReturnsEmptyList()
    {
        var sut = new GetUserAiKeys(_repo);
        _repo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new List<UserAiKey>());

        var result = await sut.ExecuteAsync(new GetUserAiKeys.Query(_userId));

        Assert.Empty(result);
    }

    [Fact]
    public async Task Get_WithMultipleProviders_ReturnsAll()
    {
        var sut = new GetUserAiKeys(_repo);
        var keys = new List<UserAiKey>
        {
            UserAiKey.Create(_userId, AiProvider.Anthropic, "enc1", "h1", null),
            UserAiKey.Create(_userId, AiProvider.OpenAI, "enc2", "h2", "gpt-4"),
            UserAiKey.Create(_userId, AiProvider.Gemini, "enc3", "h3", "gemini-pro"),
        };
        _repo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(keys);

        var result = await sut.ExecuteAsync(new GetUserAiKeys.Query(_userId));

        Assert.Equal(3, result.Count);
    }

    #endregion

    #region DeleteUserAiKey

    [Fact]
    public async Task Delete_ExistingKey_RemovesAndSaves()
    {
        var sut = new DeleteUserAiKey(_repo, _unitOfWork);
        var key = UserAiKey.Create(_userId, AiProvider.Anthropic, "enc", "hint", null);
        _repo.GetByUserAndProviderAsync(_userId, AiProvider.Anthropic, Arg.Any<CancellationToken>())
            .Returns(key);

        await sut.ExecuteAsync(new DeleteUserAiKey.Command(_userId, AiProvider.Anthropic));

        _repo.Received(1).Remove(key);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_NonExistentKey_ThrowsUserAiKeyNotFoundException()
    {
        var sut = new DeleteUserAiKey(_repo, _unitOfWork);
        _repo.GetByUserAndProviderAsync(_userId, AiProvider.OpenAI, Arg.Any<CancellationToken>())
            .Returns((UserAiKey?)null);

        var ex = await Assert.ThrowsAsync<UserAiKeyNotFoundException>(
            () => sut.ExecuteAsync(new DeleteUserAiKey.Command(_userId, AiProvider.OpenAI)));

        Assert.Equal(_userId, ex.UserId);
        Assert.Equal(AiProvider.OpenAI, ex.Provider);
    }

    [Fact]
    public async Task Delete_NonExistentKey_DoesNotCallSave()
    {
        var sut = new DeleteUserAiKey(_repo, _unitOfWork);
        _repo.GetByUserAndProviderAsync(_userId, AiProvider.Gemini, Arg.Any<CancellationToken>())
            .Returns((UserAiKey?)null);

        await Assert.ThrowsAsync<UserAiKeyNotFoundException>(
            () => sut.ExecuteAsync(new DeleteUserAiKey.Command(_userId, AiProvider.Gemini)));

        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    #endregion
}
