using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Domain.Tests.ValueObjects;

public class CheckboxRefTests
{
    [Fact]
    public void Constructor_WithValidInputs_SetsProperties()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var checkboxId = "checkbox-1";

        // Act
        var checkboxRef = new CheckboxRef(noteId, checkboxId);

        // Assert
        Assert.Equal(noteId, checkboxRef.NoteId);
        Assert.Equal(checkboxId, checkboxRef.CheckboxId);
    }

    [Fact]
    public void IsLinked_WithValidNoteIdAndCheckboxId_ReturnsTrue()
    {
        // Arrange
        var checkboxRef = new CheckboxRef(Guid.NewGuid(), "checkbox-1");

        // Act & Assert
        Assert.True(checkboxRef.IsLinked);
    }

    [Fact]
    public void IsLinked_WithEmptyNoteId_ReturnsFalse()
    {
        // Arrange
        var checkboxRef = new CheckboxRef(Guid.Empty, "checkbox-1");

        // Act & Assert
        Assert.False(checkboxRef.IsLinked);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsLinked_WithNullOrEmptyCheckboxId_ReturnsFalse(string? invalidCheckboxId)
    {
        // Arrange
        var checkboxRef = new CheckboxRef(Guid.NewGuid(), invalidCheckboxId!);

        // Act & Assert
        Assert.False(checkboxRef.IsLinked);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var checkboxRef1 = new CheckboxRef(noteId, "checkbox-1");
        var checkboxRef2 = new CheckboxRef(noteId, "checkbox-1");

        // Act & Assert
        Assert.Equal(checkboxRef1, checkboxRef2);
    }

    [Fact]
    public void Equality_DifferentNoteId_AreNotEqual()
    {
        // Arrange
        var checkboxRef1 = new CheckboxRef(Guid.NewGuid(), "checkbox-1");
        var checkboxRef2 = new CheckboxRef(Guid.NewGuid(), "checkbox-1");

        // Act & Assert
        Assert.NotEqual(checkboxRef1, checkboxRef2);
    }

    [Fact]
    public void Equality_DifferentCheckboxId_AreNotEqual()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var checkboxRef1 = new CheckboxRef(noteId, "checkbox-1");
        var checkboxRef2 = new CheckboxRef(noteId, "checkbox-2");

        // Act & Assert
        Assert.NotEqual(checkboxRef1, checkboxRef2);
    }

    [Fact]
    public void ToString_ReturnsNoteIdColonCheckboxId()
    {
        // Arrange
        var noteId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var checkboxRef = new CheckboxRef(noteId, "checkbox-1");

        // Act & Assert
        Assert.Equal("12345678-1234-1234-1234-123456789012:checkbox-1", checkboxRef.ToString());
    }
}
