using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Domain.Tests.Aggregates;

public class UserAiKeyTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private const string ValidEncryptedKey = "encrypted-key-data";
    private const string ValidKeyHint = "sk-ant-...a3kX";
    private const string ValidModel = "claude-sonnet-4-6";

    #region Create Tests

    [Fact]
    public void Create_WithValidInputs_CreatesKeyWithCorrectProperties()
    {
        // Act
        var key = UserAiKey.Create(_validUserId, AiProvider.Anthropic, ValidEncryptedKey, ValidKeyHint, ValidModel);

        // Assert
        Assert.NotEqual(Guid.Empty, key.Id);
        Assert.Equal(_validUserId, key.UserId);
        Assert.Equal(AiProvider.Anthropic, key.Provider);
        Assert.Equal(ValidEncryptedKey, key.EncryptedKey);
        Assert.Equal(ValidKeyHint, key.KeyHint);
        Assert.Equal(ValidModel, key.PreferredModel);
        Assert.True(key.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(key.UpdatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_WithNullPreferredModel_CreatesKeyWithNullModel()
    {
        // Act
        var key = UserAiKey.Create(_validUserId, AiProvider.OpenAI, ValidEncryptedKey, ValidKeyHint, null);

        // Assert
        Assert.Null(key.PreferredModel);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserAiKey.Create(Guid.Empty, AiProvider.Anthropic, ValidEncryptedKey, ValidKeyHint, ValidModel));
    }

    [Fact]
    public void Create_WithNullEncryptedKey_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            UserAiKey.Create(_validUserId, AiProvider.Anthropic, null!, ValidKeyHint, ValidModel));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceEncryptedKey_ThrowsArgumentException(string invalidKey)
    {
        Assert.Throws<ArgumentException>(() =>
            UserAiKey.Create(_validUserId, AiProvider.Anthropic, invalidKey, ValidKeyHint, ValidModel));
    }

    [Fact]
    public void Create_WithNullKeyHint_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            UserAiKey.Create(_validUserId, AiProvider.Anthropic, ValidEncryptedKey, null!, ValidModel));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceKeyHint_ThrowsArgumentException(string invalidHint)
    {
        Assert.Throws<ArgumentException>(() =>
            UserAiKey.Create(_validUserId, AiProvider.Anthropic, ValidEncryptedKey, invalidHint, ValidModel));
    }

    [Theory]
    [InlineData(AiProvider.Anthropic)]
    [InlineData(AiProvider.OpenAI)]
    [InlineData(AiProvider.Gemini)]
    public void Create_WithEachProvider_SetsProviderCorrectly(AiProvider provider)
    {
        // Act
        var key = UserAiKey.Create(_validUserId, provider, ValidEncryptedKey, ValidKeyHint, null);

        // Assert
        Assert.Equal(provider, key.Provider);
    }

    #endregion

    #region UpdateKey Tests

    [Fact]
    public void UpdateKey_WithValidInputs_UpdatesAllFields()
    {
        // Arrange
        var key = UserAiKey.Create(_validUserId, AiProvider.Anthropic, ValidEncryptedKey, ValidKeyHint, ValidModel);
        var originalUpdatedAt = key.UpdatedAt;

        var newEncrypted = "new-encrypted-key";
        var newHint = "sk-new-...xYz1";
        var newModel = "claude-opus-4-6";

        // Act
        key.UpdateKey(newEncrypted, newHint, newModel);

        // Assert
        Assert.Equal(newEncrypted, key.EncryptedKey);
        Assert.Equal(newHint, key.KeyHint);
        Assert.Equal(newModel, key.PreferredModel);
        Assert.True(key.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateKey_WithNullModel_SetsModelToNull()
    {
        // Arrange
        var key = UserAiKey.Create(_validUserId, AiProvider.Anthropic, ValidEncryptedKey, ValidKeyHint, ValidModel);

        // Act
        key.UpdateKey("new-encrypted", "new-hint", null);

        // Assert
        Assert.Null(key.PreferredModel);
    }

    [Fact]
    public void UpdateKey_WithNullEncryptedKey_ThrowsArgumentNullException()
    {
        var key = UserAiKey.Create(_validUserId, AiProvider.Anthropic, ValidEncryptedKey, ValidKeyHint, ValidModel);

        Assert.Throws<ArgumentNullException>(() =>
            key.UpdateKey(null!, ValidKeyHint, ValidModel));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateKey_WithEmptyOrWhitespaceEncryptedKey_ThrowsArgumentException(string invalidKey)
    {
        var key = UserAiKey.Create(_validUserId, AiProvider.Anthropic, ValidEncryptedKey, ValidKeyHint, ValidModel);

        Assert.Throws<ArgumentException>(() =>
            key.UpdateKey(invalidKey, ValidKeyHint, ValidModel));
    }

    [Fact]
    public void UpdateKey_WithNullKeyHint_ThrowsArgumentNullException()
    {
        var key = UserAiKey.Create(_validUserId, AiProvider.Anthropic, ValidEncryptedKey, ValidKeyHint, ValidModel);

        Assert.Throws<ArgumentNullException>(() =>
            key.UpdateKey(ValidEncryptedKey, null!, ValidModel));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateKey_WithEmptyOrWhitespaceKeyHint_ThrowsArgumentException(string invalidHint)
    {
        var key = UserAiKey.Create(_validUserId, AiProvider.Anthropic, ValidEncryptedKey, ValidKeyHint, ValidModel);

        Assert.Throws<ArgumentException>(() =>
            key.UpdateKey(ValidEncryptedKey, invalidHint, ValidModel));
    }

    #endregion

    #region UpdateModel Tests

    [Fact]
    public void UpdateModel_WithNewModel_UpdatesModelAndTimestamp()
    {
        // Arrange
        var key = UserAiKey.Create(_validUserId, AiProvider.Anthropic, ValidEncryptedKey, ValidKeyHint, ValidModel);
        var originalUpdatedAt = key.UpdatedAt;

        // Act
        key.UpdateModel("gpt-4o");

        // Assert
        Assert.Equal("gpt-4o", key.PreferredModel);
        Assert.True(key.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateModel_WithNull_ClearsModel()
    {
        // Arrange
        var key = UserAiKey.Create(_validUserId, AiProvider.Anthropic, ValidEncryptedKey, ValidKeyHint, ValidModel);

        // Act
        key.UpdateModel(null);

        // Assert
        Assert.Null(key.PreferredModel);
    }

    [Fact]
    public void UpdateModel_DoesNotChangeEncryptedKeyOrHint()
    {
        // Arrange
        var key = UserAiKey.Create(_validUserId, AiProvider.Anthropic, ValidEncryptedKey, ValidKeyHint, ValidModel);

        // Act
        key.UpdateModel("new-model");

        // Assert
        Assert.Equal(ValidEncryptedKey, key.EncryptedKey);
        Assert.Equal(ValidKeyHint, key.KeyHint);
    }

    #endregion
}
