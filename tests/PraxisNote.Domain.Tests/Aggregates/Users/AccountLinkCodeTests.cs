using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Domain.Tests.Aggregates.Users;

public class AccountLinkCodeTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private const string ValidCodeHash = "abc123def456";

    #region Create

    [Fact]
    public void Create_WithValidInputs_ReturnsAccountLinkCodeWithCorrectProperties()
    {
        // Arrange
        var expiry = TimeSpan.FromMinutes(15);

        // Act
        var code = AccountLinkCode.Create(_validUserId, ValidCodeHash, expiry);

        // Assert
        Assert.NotEqual(Guid.Empty, code.Id);
        Assert.Equal(_validUserId, code.UserId);
        Assert.Equal(ValidCodeHash, code.CodeHash);
        Assert.False(code.IsRedeemed);
        Assert.True(code.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(code.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.True(code.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AccountLinkCode.Create(Guid.Empty, ValidCodeHash, TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public void Create_WithNullCodeHash_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AccountLinkCode.Create(_validUserId, null!, TimeSpan.FromMinutes(15)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceCodeHash_ThrowsArgumentException(string invalidHash)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            AccountLinkCode.Create(_validUserId, invalidHash, TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public void Create_WithZeroExpiry_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AccountLinkCode.Create(_validUserId, ValidCodeHash, TimeSpan.Zero));
    }

    [Fact]
    public void Create_WithNegativeExpiry_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AccountLinkCode.Create(_validUserId, ValidCodeHash, TimeSpan.FromMinutes(-5)));
    }

    #endregion

    #region IsExpired

    [Fact]
    public void IsExpired_WhenNotExpired_ReturnsFalse()
    {
        // Arrange
        var code = AccountLinkCode.Create(_validUserId, ValidCodeHash, TimeSpan.FromMinutes(15));

        // Act & Assert
        Assert.False(code.IsExpired());
    }

    [Fact]
    public void IsExpired_WhenCreatedWithVeryShortExpiry_ReturnsTrue()
    {
        // Arrange - use a very short expiry that will have passed by assertion time
        // We test this indirectly through IsValid with MarkRedeemed
        var code = AccountLinkCode.Create(_validUserId, ValidCodeHash, TimeSpan.FromMilliseconds(1));

        // Allow the code to expire
        Thread.Sleep(10);

        // Act & Assert
        Assert.True(code.IsExpired());
    }

    #endregion

    #region IsValid

    [Fact]
    public void IsValid_WhenNotRedeemedAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var code = AccountLinkCode.Create(_validUserId, ValidCodeHash, TimeSpan.FromMinutes(15));

        // Act & Assert
        Assert.True(code.IsValid());
    }

    [Fact]
    public void IsValid_WhenRedeemed_ReturnsFalse()
    {
        // Arrange
        var code = AccountLinkCode.Create(_validUserId, ValidCodeHash, TimeSpan.FromMinutes(15));
        code.MarkRedeemed();

        // Act & Assert
        Assert.False(code.IsValid());
    }

    [Fact]
    public void IsValid_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var code = AccountLinkCode.Create(_validUserId, ValidCodeHash, TimeSpan.FromMilliseconds(1));

        // Allow the code to expire
        Thread.Sleep(10);

        // Act & Assert
        Assert.False(code.IsValid());
    }

    #endregion

    #region MarkRedeemed

    [Fact]
    public void MarkRedeemed_SetsIsRedeemedToTrue()
    {
        // Arrange
        var code = AccountLinkCode.Create(_validUserId, ValidCodeHash, TimeSpan.FromMinutes(15));

        // Act
        code.MarkRedeemed();

        // Assert
        Assert.True(code.IsRedeemed);
    }

    [Fact]
    public void MarkRedeemed_WhenAlreadyRedeemed_RemainsRedeemed()
    {
        // Arrange
        var code = AccountLinkCode.Create(_validUserId, ValidCodeHash, TimeSpan.FromMinutes(15));
        code.MarkRedeemed();

        // Act
        code.MarkRedeemed();

        // Assert
        Assert.True(code.IsRedeemed);
    }

    #endregion
}
