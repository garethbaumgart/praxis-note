using PraxisNote.Domain.Aggregates.Labels;

namespace PraxisNote.Domain.Tests.Aggregates;

public class LabelTests
{
    private readonly Guid _validUserId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInputs_ReturnsLabelWithCorrectProperties()
    {
        // Arrange
        var name = "Work";

        // Act
        var label = Label.Create(_validUserId, name);

        // Assert
        Assert.NotEqual(Guid.Empty, label.Id);
        Assert.Equal(_validUserId, label.UserId);
        Assert.Equal(name, label.Name);
        Assert.True(label.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Label.Create(Guid.Empty, "Work"));
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => Label.Create(_validUserId, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => Label.Create(_validUserId, invalidName));
    }

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        // Arrange
        var label = Label.Create(_validUserId, "Old Name");

        // Act
        label.Rename("New Name");

        // Assert
        Assert.Equal("New Name", label.Name);
    }

    [Fact]
    public void Rename_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var label = Label.Create(_validUserId, "Valid Name");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => label.Rename(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Arrange
        var label = Label.Create(_validUserId, "Valid Name");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => label.Rename(invalidName));
    }
}
