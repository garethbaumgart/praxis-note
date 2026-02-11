using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Domain.Tests.Aggregates.Users;

public class LinkedIdentityTests
{
    private readonly Guid _validUserId = Guid.NewGuid();

    #region Create

    [Fact]
    public void Create_WithValidInputs_ReturnsLinkedIdentityWithCorrectProperties()
    {
        // Arrange & Act
        var identity = LinkedIdentity.Create(
            _validUserId, "google", "provider-id-123",
            "test@example.com", "Test User");

        // Assert
        Assert.NotEqual(Guid.Empty, identity.Id);
        Assert.Equal(_validUserId, identity.UserId);
        Assert.Equal("google", identity.Provider);
        Assert.Equal("provider-id-123", identity.ProviderId);
        Assert.Equal("test@example.com", identity.Email);
        Assert.Equal("Test User", identity.Name);
        Assert.Null(identity.AvatarUrl);
        Assert.Null(identity.DefaultProfileId);
        Assert.True(identity.LinkedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_WithAvatarUrl_SetsAvatarUrl()
    {
        // Arrange & Act
        var identity = LinkedIdentity.Create(
            _validUserId, "google", "provider-id-123",
            "test@example.com", "Test User",
            avatarUrl: "https://example.com/avatar.jpg");

        // Assert
        Assert.Equal("https://example.com/avatar.jpg", identity.AvatarUrl);
    }

    [Fact]
    public void Create_WithDefaultProfileId_SetsDefaultProfileId()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        // Act
        var identity = LinkedIdentity.Create(
            _validUserId, "google", "provider-id-123",
            "test@example.com", "Test User",
            defaultProfileId: profileId);

        // Assert
        Assert.Equal(profileId, identity.DefaultProfileId);
    }

    [Fact]
    public void Create_NormalizesProviderToLowercase()
    {
        // Arrange & Act
        var identity = LinkedIdentity.Create(
            _validUserId, "GOOGLE", "provider-id-123",
            "test@example.com", "Test User");

        // Assert
        Assert.Equal("google", identity.Provider);
    }

    [Fact]
    public void Create_NormalizesEmailToLowercase()
    {
        // Arrange & Act
        var identity = LinkedIdentity.Create(
            _validUserId, "google", "provider-id-123",
            "Test@Example.COM", "Test User");

        // Assert
        Assert.Equal("test@example.com", identity.Email);
    }

    [Fact]
    public void Create_TrimsProviderAndProviderId()
    {
        // Arrange & Act
        var identity = LinkedIdentity.Create(
            _validUserId, "  google  ", "  provider-id-123  ",
            "test@example.com", "Test User");

        // Assert
        Assert.Equal("google", identity.Provider);
        Assert.Equal("provider-id-123", identity.ProviderId);
    }

    [Fact]
    public void Create_WithWhitespaceOnlyAvatarUrl_SetsNull()
    {
        // Arrange & Act
        var identity = LinkedIdentity.Create(
            _validUserId, "google", "provider-id-123",
            "test@example.com", "Test User",
            avatarUrl: "   ");

        // Assert
        Assert.Null(identity.AvatarUrl);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LinkedIdentity.Create(Guid.Empty, "google", "id", "e@e.com", "Name"));
    }

    [Fact]
    public void Create_WithNullProvider_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            LinkedIdentity.Create(_validUserId, null!, "id", "e@e.com", "Name"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceProvider_ThrowsArgumentException(string invalidProvider)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            LinkedIdentity.Create(_validUserId, invalidProvider, "id", "e@e.com", "Name"));
    }

    [Fact]
    public void Create_WithNullProviderId_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            LinkedIdentity.Create(_validUserId, "google", null!, "e@e.com", "Name"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceProviderId_ThrowsArgumentException(string invalidProviderId)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            LinkedIdentity.Create(_validUserId, "google", invalidProviderId, "e@e.com", "Name"));
    }

    [Fact]
    public void Create_WithNullEmail_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            LinkedIdentity.Create(_validUserId, "google", "id", null!, "Name"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceEmail_ThrowsArgumentException(string invalidEmail)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            LinkedIdentity.Create(_validUserId, "google", "id", invalidEmail, "Name"));
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            LinkedIdentity.Create(_validUserId, "google", "id", "e@e.com", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            LinkedIdentity.Create(_validUserId, "google", "id", "e@e.com", invalidName));
    }

    #endregion

    #region SetDefaultProfile

    [Fact]
    public void SetDefaultProfile_WithProfileId_SetsDefaultProfileId()
    {
        // Arrange
        var identity = LinkedIdentity.Create(
            _validUserId, "google", "id", "e@e.com", "Name");
        var profileId = Guid.NewGuid();

        // Act
        identity.SetDefaultProfile(profileId);

        // Assert
        Assert.Equal(profileId, identity.DefaultProfileId);
    }

    [Fact]
    public void SetDefaultProfile_WithNull_ClearsDefaultProfileId()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var identity = LinkedIdentity.Create(
            _validUserId, "google", "id", "e@e.com", "Name",
            defaultProfileId: profileId);

        // Act
        identity.SetDefaultProfile(null);

        // Assert
        Assert.Null(identity.DefaultProfileId);
    }

    #endregion
}
