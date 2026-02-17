using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.ApiKeys;
using PraxisNote.Domain.Aggregates.ApiKeys;

namespace PraxisNote.Application.Tests.ApiKeys;

public class CreateApiKeyTests
{
    private readonly IApiKeyRepository _apiKeyRepo = Substitute.For<IApiKeyRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateApiKey _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public CreateApiKeyTests()
    {
        _sut = new CreateApiKey(_apiKeyRepo, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCommand_CreatesApiKey()
    {
        // Arrange
        _apiKeyRepo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new List<ApiKey>());

        var command = new CreateApiKey.Command(_userId, _profileId, "Test Key");

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result.ApiKeyId);
        Assert.StartsWith("pn_", result.RawKey);
        Assert.NotEmpty(result.Prefix);
        await _apiKeyRepo.Received(1).AddAsync(Arg.Any<ApiKey>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithExpiresAt_CreatesApiKeyWithExpiration()
    {
        // Arrange
        _apiKeyRepo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new List<ApiKey>());

        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);
        var command = new CreateApiKey.Command(_userId, _profileId, "Test Key", expiresAt);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result.ApiKeyId);
        await _apiKeyRepo.Received(1).AddAsync(
            Arg.Is<ApiKey>(k => k.ExpiresAt == expiresAt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_AtMaxActiveKeys_ThrowsTooManyKeysError()
    {
        // Arrange — create 5 active (non-revoked, non-expired) keys
        var existingKeys = new List<ApiKey>();
        for (var i = 0; i < CreateApiKey.MaxKeysPerUser; i++)
        {
            var (apiKey, _) = ApiKey.Create(_userId, _profileId, $"Key {i}");
            existingKeys.Add(apiKey);
        }

        _apiKeyRepo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(existingKeys);

        var command = new CreateApiKey.Command(_userId, _profileId, "One Too Many");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(command));

        Assert.Equal(CreateApiKey.TooManyKeysError, ex.Message);
        await _apiKeyRepo.DidNotReceive().AddAsync(Arg.Any<ApiKey>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithRevokedKeysNotCountingTowardLimit_AllowsCreation()
    {
        // Arrange — 5 keys but all revoked, so active count is 0
        var existingKeys = new List<ApiKey>();
        for (var i = 0; i < CreateApiKey.MaxKeysPerUser; i++)
        {
            var (apiKey, _) = ApiKey.Create(_userId, _profileId, $"Key {i}");
            apiKey.Revoke();
            existingKeys.Add(apiKey);
        }

        _apiKeyRepo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(existingKeys);

        var command = new CreateApiKey.Command(_userId, _profileId, "New Key");

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result.ApiKeyId);
        await _apiKeyRepo.Received(1).AddAsync(Arg.Any<ApiKey>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithExpiredKeysNotCountingTowardLimit_AllowsCreation()
    {
        // Arrange — 5 keys but all expired, so active count is 0
        var existingKeys = new List<ApiKey>();
        for (var i = 0; i < CreateApiKey.MaxKeysPerUser; i++)
        {
            var (apiKey, _) = ApiKey.Create(_userId, _profileId, $"Key {i}",
                DateTimeOffset.UtcNow.AddSeconds(-1));
            existingKeys.Add(apiKey);
        }

        _apiKeyRepo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(existingKeys);

        var command = new CreateApiKey.Command(_userId, _profileId, "New Key");

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result.ApiKeyId);
        await _apiKeyRepo.Received(1).AddAsync(Arg.Any<ApiKey>(), Arg.Any<CancellationToken>());
    }
}
