using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@example.co.uk")]
    public void Constructor_WithValidEmail_CreatesEmail(string validEmail)
    {
        // Act
        var email = new Email(validEmail);

        // Assert
        Assert.Equal(validEmail.ToLowerInvariant(), email.Value);
    }

    [Fact]
    public void Constructor_WithNullEmail_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Email(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespaceEmail_ThrowsArgumentException(string invalidEmail)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Email(invalidEmail));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    public void Constructor_WithInvalidFormat_ThrowsArgumentException(string invalidEmail)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Email(invalidEmail));
        Assert.Contains("Invalid email format", ex.Message);
    }

    [Fact]
    public void Value_IsStoredAsLowercase()
    {
        // Arrange
        var mixedCaseEmail = "Test.User@Example.COM";

        // Act
        var email = new Email(mixedCaseEmail);

        // Assert
        Assert.Equal("test.user@example.com", email.Value);
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        // Arrange
        var email1 = new Email("Test@Example.com");
        var email2 = new Email("test@example.com");

        // Act & Assert
        Assert.Equal(email1, email2);
    }

    [Fact]
    public void ToString_ReturnsLowercaseEmailAddress()
    {
        // Arrange
        var email = new Email("Test@Example.COM");

        // Act & Assert
        Assert.Equal("test@example.com", email.ToString());
    }

    [Fact]
    public void Constructor_TrimsWhitespaceFromEmail()
    {
        // Arrange & Act
        var email = new Email("  test@example.com  ");

        // Assert
        Assert.Equal("test@example.com", email.Value);
    }
}
