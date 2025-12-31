using PraxisNote.Domain.Aggregates.Users;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Domain.Tests.Aggregates;

public class UserTests
{
    private readonly ExternalIdentity _validExternalIdentity = new("Google", "oauth-123");
    private readonly Email _validEmail = new("test@example.com");
    private readonly string _validName = "Test User";

    [Fact]
    public void Register_WithValidInputs_ReturnsUserWithCorrectProperties()
    {
        // Act
        var user = User.Register(_validExternalIdentity, _validEmail, _validName);

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(_validExternalIdentity, user.ExternalIdentity);
        Assert.Equal(_validEmail, user.Email);
        Assert.Equal(_validName, user.Name);
        Assert.Null(user.AvatarUrl);
        Assert.True(user.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(user.LastLoginAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Register_WithAvatarUrl_SetsAvatarUrl()
    {
        // Arrange
        var avatarUrl = "https://example.com/avatar.jpg";

        // Act
        var user = User.Register(_validExternalIdentity, _validEmail, _validName, avatarUrl);

        // Assert
        Assert.Equal(avatarUrl, user.AvatarUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithEmptyOrWhitespaceAvatarUrl_SetsAvatarUrlToNull(string emptyAvatarUrl)
    {
        // Act
        var user = User.Register(_validExternalIdentity, _validEmail, _validName, emptyAvatarUrl);

        // Assert
        Assert.Null(user.AvatarUrl);
    }

    [Fact]
    public void Register_WithNullExternalIdentity_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => User.Register(null!, _validEmail, _validName));
    }

    [Fact]
    public void Register_WithNullEmail_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => User.Register(_validExternalIdentity, null!, _validName));
    }

    [Fact]
    public void Register_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => User.Register(_validExternalIdentity, _validEmail, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => User.Register(_validExternalIdentity, _validEmail, invalidName));
    }

    [Fact]
    public void Register_SetsCreatedAtAndLastLoginAtToSameTime()
    {
        // Act
        var user = User.Register(_validExternalIdentity, _validEmail, _validName);

        // Assert
        Assert.Equal(user.CreatedAt, user.LastLoginAt);
    }

    [Fact]
    public void RecordLogin_UpdatesLastLoginAt()
    {
        // Arrange
        var user = User.Register(_validExternalIdentity, _validEmail, _validName);
        var originalLoginAt = user.LastLoginAt;

        // Act
        user.RecordLogin();

        // Assert
        Assert.True(user.LastLoginAt >= originalLoginAt);
    }

    [Fact]
    public void RecordLogin_DoesNotChangeCreatedAt()
    {
        // Arrange
        var user = User.Register(_validExternalIdentity, _validEmail, _validName);
        var originalCreatedAt = user.CreatedAt;

        // Act
        user.RecordLogin();

        // Assert
        Assert.Equal(originalCreatedAt, user.CreatedAt);
    }
}
