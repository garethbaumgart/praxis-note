using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Domain.Tests.Aggregates;

public class TagTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private const string ValidColor = "#3b82f6";

    #region Create Tests

    [Fact]
    public void Create_WithValidInputs_ReturnsTagWithCorrectProperties()
    {
        // Arrange
        var name = "Work";

        // Act
        var tag = Tag.Create(_validUserId, name, ValidColor);

        // Assert
        Assert.NotEqual(Guid.Empty, tag.Id);
        Assert.Equal(_validUserId, tag.UserId);
        Assert.Equal(name, tag.Name);
        Assert.Equal(ValidColor, tag.Color);
        Assert.True(tag.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Tag.Create(Guid.Empty, "Work", ValidColor));
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => Tag.Create(_validUserId, null!, ValidColor));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => Tag.Create(_validUserId, invalidName, ValidColor));
    }

    [Fact]
    public void Create_WithNullColor_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => Tag.Create(_validUserId, "Work", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceColor_ThrowsArgumentException(string invalidColor)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => Tag.Create(_validUserId, "Work", invalidColor));
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#fff")]
    [InlineData("3b82f6")]
    [InlineData("#3b82f")]
    [InlineData("#3b82f6g")]
    [InlineData("rgb(0,0,0)")]
    public void Create_WithInvalidColorFormat_ThrowsArgumentException(string invalidColor)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => Tag.Create(_validUserId, "Work", invalidColor));
    }

    [Theory]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData("#3b82f6")]
    [InlineData("#A3BE8C")]
    public void Create_WithValidColorFormats_Succeeds(string validColor)
    {
        // Arrange & Act
        var tag = Tag.Create(_validUserId, "Work", validColor);

        // Assert
        Assert.Equal(validColor, tag.Color);
    }

    #endregion

    #region Rename Tests

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        // Arrange
        var tag = Tag.Create(_validUserId, "Old Name", ValidColor);

        // Act
        tag.Rename("New Name");

        // Assert
        Assert.Equal("New Name", tag.Name);
    }

    [Fact]
    public void Rename_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var tag = Tag.Create(_validUserId, "Valid Name", ValidColor);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tag.Rename(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Arrange
        var tag = Tag.Create(_validUserId, "Valid Name", ValidColor);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tag.Rename(invalidName));
    }

    #endregion

    #region Recolor Tests

    [Fact]
    public void Recolor_WithValidColor_UpdatesColor()
    {
        // Arrange
        var tag = Tag.Create(_validUserId, "Work", "#000000");

        // Act
        tag.Recolor("#ffffff");

        // Assert
        Assert.Equal("#ffffff", tag.Color);
    }

    [Fact]
    public void Recolor_WithNullColor_ThrowsArgumentNullException()
    {
        // Arrange
        var tag = Tag.Create(_validUserId, "Work", ValidColor);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tag.Recolor(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recolor_WithEmptyOrWhitespaceColor_ThrowsArgumentException(string invalidColor)
    {
        // Arrange
        var tag = Tag.Create(_validUserId, "Work", ValidColor);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tag.Recolor(invalidColor));
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#fff")]
    [InlineData("3b82f6")]
    public void Recolor_WithInvalidColorFormat_ThrowsArgumentException(string invalidColor)
    {
        // Arrange
        var tag = Tag.Create(_validUserId, "Work", ValidColor);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tag.Recolor(invalidColor));
    }

    #endregion
}
