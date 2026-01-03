using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Domain.Tests.ValueObjects;

public class ExternalIdentityTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesExternalIdentity()
    {
        // Act
        var identity = new ExternalIdentity("Google", "123456");

        // Assert
        Assert.Equal("google", identity.Provider);
        Assert.Equal("123456", identity.ProviderId);
    }

    [Fact]
    public void Constructor_NormalizesProviderToLowercase()
    {
        // Act
        var identity = new ExternalIdentity("GOOGLE", "123456");

        // Assert
        Assert.Equal("google", identity.Provider);
    }

    [Fact]
    public void Equality_ProviderIsCaseInsensitive()
    {
        // Arrange
        var identity1 = new ExternalIdentity("Google", "123456");
        var identity2 = new ExternalIdentity("GOOGLE", "123456");

        // Act & Assert
        Assert.Equal(identity1, identity2);
    }

    [Fact]
    public void Constructor_TrimsWhitespaceFromProviderAndProviderId()
    {
        // Act
        var identity = new ExternalIdentity("  Google  ", "  123456  ");

        // Assert
        Assert.Equal("google", identity.Provider);
        Assert.Equal("123456", identity.ProviderId);
    }

    [Fact]
    public void Constructor_WithNullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ExternalIdentity(null!, "123456"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespaceProvider_ThrowsArgumentException(string invalidProvider)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ExternalIdentity(invalidProvider, "123456"));
    }

    [Fact]
    public void Constructor_WithNullProviderId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ExternalIdentity("Google", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespaceProviderId_ThrowsArgumentException(string invalidProviderId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ExternalIdentity("Google", invalidProviderId));
    }

    [Fact]
    public void Equality_SameProviderAndId_AreEqual()
    {
        // Arrange
        var identity1 = new ExternalIdentity("Google", "123456");
        var identity2 = new ExternalIdentity("Google", "123456");

        // Act & Assert
        Assert.Equal(identity1, identity2);
    }

    [Fact]
    public void Equality_DifferentProvider_AreNotEqual()
    {
        // Arrange
        var identity1 = new ExternalIdentity("Google", "123456");
        var identity2 = new ExternalIdentity("Microsoft", "123456");

        // Act & Assert
        Assert.NotEqual(identity1, identity2);
    }

    [Fact]
    public void Equality_DifferentProviderId_AreNotEqual()
    {
        // Arrange
        var identity1 = new ExternalIdentity("Google", "123456");
        var identity2 = new ExternalIdentity("Google", "789012");

        // Act & Assert
        Assert.NotEqual(identity1, identity2);
    }

    [Fact]
    public void ToString_ReturnsLowercaseProviderColonProviderId()
    {
        // Arrange
        var identity = new ExternalIdentity("Google", "123456");

        // Act & Assert
        Assert.Equal("google:123456", identity.ToString());
    }
}
