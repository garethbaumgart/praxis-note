using PraxisNote.Domain.Aggregates.ApiKeys;

namespace PraxisNote.Domain.Tests.Aggregates;

public class ApiKeyTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProfileId = Guid.NewGuid();
    private readonly string _validName = "My API Key";

    #region Create Tests

    [Fact]
    public void Create_WithValidInputs_ReturnsApiKeyAndRawKey()
    {
        // Act
        var (apiKey, rawKey) = ApiKey.Create(_validUserId, _validProfileId, _validName);

        // Assert
        Assert.NotEqual(Guid.Empty, apiKey.Id);
        Assert.Equal(_validUserId, apiKey.UserId);
        Assert.Equal(_validProfileId, apiKey.ProfileId);
        Assert.Equal(_validName, apiKey.Name);
        Assert.False(apiKey.IsRevoked);
        Assert.Null(apiKey.LastUsedAt);
        Assert.Null(apiKey.ExpiresAt);
        Assert.True(apiKey.IsValid);
        Assert.False(apiKey.IsExpired);
    }

    [Fact]
    public void Create_RawKeyStartsWithPrefix()
    {
        // Act
        var (_, rawKey) = ApiKey.Create(_validUserId, _validProfileId, _validName);

        // Assert
        Assert.StartsWith("pn_", rawKey);
    }

    [Fact]
    public void Create_KeyPrefixMatchesRawKeyPrefix()
    {
        // Act
        var (apiKey, rawKey) = ApiKey.Create(_validUserId, _validProfileId, _validName);

        // Assert
        Assert.Equal(rawKey[..11], apiKey.KeyPrefix);
    }

    [Fact]
    public void Create_KeyHashIsNotEmpty()
    {
        // Act
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName);

        // Assert
        Assert.NotEmpty(apiKey.KeyHash);
        Assert.Equal(64, apiKey.KeyHash.Length); // SHA256 hex string
    }

    [Fact]
    public void Create_TrimsName()
    {
        // Act
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, "  Trimmed Key  ");

        // Assert
        Assert.Equal("Trimmed Key", apiKey.Name);
    }

    [Fact]
    public void Create_WithExpiresAt_SetsExpiresAt()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName, expiresAt);

        // Assert
        Assert.Equal(expiresAt, apiKey.ExpiresAt);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApiKey.Create(Guid.Empty, _validProfileId, _validName));
    }

    [Fact]
    public void Create_WithEmptyProfileId_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApiKey.Create(_validUserId, Guid.Empty, _validName));
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ApiKey.Create(_validUserId, _validProfileId, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ApiKey.Create(_validUserId, _validProfileId, invalidName));
    }

    [Fact]
    public void Create_GeneratesUniqueKeysEachTime()
    {
        // Act
        var (apiKey1, rawKey1) = ApiKey.Create(_validUserId, _validProfileId, _validName);
        var (apiKey2, rawKey2) = ApiKey.Create(_validUserId, _validProfileId, _validName);

        // Assert
        Assert.NotEqual(rawKey1, rawKey2);
        Assert.NotEqual(apiKey1.KeyHash, apiKey2.KeyHash);
        Assert.NotEqual(apiKey1.Id, apiKey2.Id);
    }

    #endregion

    #region Revoke Tests

    [Fact]
    public void Revoke_SetsIsRevokedToTrue()
    {
        // Arrange
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName);

        // Act
        apiKey.Revoke();

        // Assert
        Assert.True(apiKey.IsRevoked);
        Assert.False(apiKey.IsValid);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_RemainsRevoked()
    {
        // Arrange
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName);
        apiKey.Revoke();

        // Act
        apiKey.Revoke();

        // Assert
        Assert.True(apiKey.IsRevoked);
    }

    #endregion

    #region Rename Tests

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        // Arrange
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName);

        // Act
        apiKey.Rename("New Name");

        // Assert
        Assert.Equal("New Name", apiKey.Name);
    }

    [Fact]
    public void Rename_TrimsName()
    {
        // Arrange
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName);

        // Act
        apiKey.Rename("  Trimmed  ");

        // Assert
        Assert.Equal("Trimmed", apiKey.Name);
    }

    [Fact]
    public void Rename_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => apiKey.Rename(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Arrange
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => apiKey.Rename(invalidName));
    }

    #endregion

    #region IsExpired Tests

    [Fact]
    public void IsExpired_WhenNoExpiresAt_ReturnsFalse()
    {
        // Act
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName);

        // Assert
        Assert.False(apiKey.IsExpired);
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInFuture_ReturnsFalse()
    {
        // Act
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName,
            DateTimeOffset.UtcNow.AddDays(30));

        // Assert
        Assert.False(apiKey.IsExpired);
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInPast_ReturnsTrue()
    {
        // We can't set ExpiresAt to the past directly through Create since it uses DateTimeOffset.UtcNow for CreatedAt,
        // but ExpiresAt can be set to any value. Let's test with a very near past time.
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName,
            DateTimeOffset.UtcNow.AddSeconds(-1));

        // Assert
        Assert.True(apiKey.IsExpired);
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_WhenNotRevokedAndNotExpired_ReturnsTrue()
    {
        // Act
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName);

        // Assert
        Assert.True(apiKey.IsValid);
    }

    [Fact]
    public void IsValid_WhenRevoked_ReturnsFalse()
    {
        // Arrange
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName);
        apiKey.Revoke();

        // Assert
        Assert.False(apiKey.IsValid);
    }

    [Fact]
    public void IsValid_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName,
            DateTimeOffset.UtcNow.AddSeconds(-1));

        // Assert
        Assert.False(apiKey.IsValid);
    }

    #endregion

    #region RecordUsage Tests

    [Fact]
    public void RecordUsage_SetsLastUsedAt()
    {
        // Arrange
        var (apiKey, _) = ApiKey.Create(_validUserId, _validProfileId, _validName);
        Assert.Null(apiKey.LastUsedAt);

        // Act
        apiKey.RecordUsage();

        // Assert
        Assert.NotNull(apiKey.LastUsedAt);
    }

    #endregion
}
