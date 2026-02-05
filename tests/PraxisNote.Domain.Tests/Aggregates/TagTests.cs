using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Domain.Tests.Aggregates;

public class TagTests
{
    private readonly Guid _validUserId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInputs_ReturnsTagWithCorrectProperties()
    {
        // Arrange
        var name = "Work";

        // Act
        var tag = Tag.Create(_validUserId, name);

        // Assert
        Assert.NotEqual(Guid.Empty, tag.Id);
        Assert.Equal(_validUserId, tag.UserId);
        Assert.Equal("work", tag.Name);
        Assert.True(tag.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Tag.Create(Guid.Empty, "Work"));
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => Tag.Create(_validUserId, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => Tag.Create(_validUserId, invalidName));
    }

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        // Arrange
        var tag = Tag.Create(_validUserId, "Old Name");

        // Act
        tag.Rename("New Name");

        // Assert
        Assert.Equal("new name", tag.Name);
    }

    [Fact]
    public void Rename_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var tag = Tag.Create(_validUserId, "Valid Name");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tag.Rename(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Arrange
        var tag = Tag.Create(_validUserId, "Valid Name");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tag.Rename(invalidName));
    }

    [Fact]
    public void Create_WithMixedCaseName_StoresAsLowercase()
    {
        // Arrange & Act
        var tag = Tag.Create(_validUserId, "Work");

        // Assert
        Assert.Equal("work", tag.Name);
    }

    [Fact]
    public void Create_WithUpperCaseName_StoresAsLowercase()
    {
        // Arrange & Act
        var tag = Tag.Create(_validUserId, "URGENT");

        // Assert
        Assert.Equal("urgent", tag.Name);
    }

    [Fact]
    public void Rename_WithMixedCaseName_StoresAsLowercase()
    {
        // Arrange
        var tag = Tag.Create(_validUserId, "old name");

        // Act
        tag.Rename("New-Name");

        // Assert
        Assert.Equal("new-name", tag.Name);
    }
}
