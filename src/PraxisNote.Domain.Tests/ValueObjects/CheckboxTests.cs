using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Domain.Tests.ValueObjects;

public class CheckboxTests
{
    private const string ValidId = "checkbox-1";
    private const string ValidText = "Complete the report";

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidInputs_CreatesCheckbox()
    {
        // Act
        var checkbox = new Checkbox(ValidId, ValidText, isChecked: false);

        // Assert
        Assert.Equal(ValidId, checkbox.Id);
        Assert.Equal(ValidText, checkbox.Text);
        Assert.False(checkbox.IsChecked);
    }

    [Fact]
    public void Constructor_WithCheckedTrue_SetsIsChecked()
    {
        // Act
        var checkbox = new Checkbox(ValidId, ValidText, isChecked: true);

        // Assert
        Assert.True(checkbox.IsChecked);
    }

    [Fact]
    public void Constructor_TrimsId()
    {
        // Act
        var checkbox = new Checkbox("  checkbox-1  ", ValidText, isChecked: false);

        // Assert
        Assert.Equal("checkbox-1", checkbox.Id);
    }

    [Fact]
    public void Constructor_TrimsText()
    {
        // Act
        var checkbox = new Checkbox(ValidId, "  Trimmed text  ", isChecked: false);

        // Assert
        Assert.Equal("Trimmed text", checkbox.Text);
    }

    [Fact]
    public void Constructor_WithNullId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Checkbox(null!, ValidText, isChecked: false));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespaceId_ThrowsArgumentException(string invalidId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Checkbox(invalidId, ValidText, isChecked: false));
    }

    [Fact]
    public void Constructor_WithNullText_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Checkbox(ValidId, null!, isChecked: false));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespaceText_ThrowsArgumentException(string invalidText)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Checkbox(ValidId, invalidText, isChecked: false));
    }

    #endregion

    #region WithText Tests

    [Fact]
    public void WithText_ReturnsNewCheckboxWithUpdatedText()
    {
        // Arrange
        var checkbox = new Checkbox(ValidId, ValidText, isChecked: true);

        // Act
        var updated = checkbox.WithText("New text");

        // Assert
        Assert.Equal("New text", updated.Text);
        Assert.Equal(checkbox.Id, updated.Id);
        Assert.Equal(checkbox.IsChecked, updated.IsChecked);
    }

    [Fact]
    public void WithText_TrimsNewText()
    {
        // Arrange
        var checkbox = new Checkbox(ValidId, ValidText, isChecked: false);

        // Act
        var updated = checkbox.WithText("  Trimmed  ");

        // Assert
        Assert.Equal("Trimmed", updated.Text);
    }

    [Fact]
    public void WithText_DoesNotModifyOriginal()
    {
        // Arrange
        var original = new Checkbox(ValidId, ValidText, isChecked: false);

        // Act
        _ = original.WithText("New text");

        // Assert
        Assert.Equal(ValidText, original.Text);
    }

    [Fact]
    public void WithText_WithNullText_ThrowsArgumentNullException()
    {
        // Arrange
        var checkbox = new Checkbox(ValidId, ValidText, isChecked: false);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => checkbox.WithText(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithText_WithEmptyOrWhitespaceText_ThrowsArgumentException(string invalidText)
    {
        // Arrange
        var checkbox = new Checkbox(ValidId, ValidText, isChecked: false);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => checkbox.WithText(invalidText));
    }

    #endregion

    #region WithChecked Tests

    [Fact]
    public void WithChecked_ReturnsNewCheckboxWithUpdatedState()
    {
        // Arrange
        var checkbox = new Checkbox(ValidId, ValidText, isChecked: false);

        // Act
        var updated = checkbox.WithChecked(true);

        // Assert
        Assert.True(updated.IsChecked);
        Assert.Equal(checkbox.Id, updated.Id);
        Assert.Equal(checkbox.Text, updated.Text);
    }

    [Fact]
    public void WithChecked_DoesNotModifyOriginal()
    {
        // Arrange
        var original = new Checkbox(ValidId, ValidText, isChecked: false);

        // Act
        _ = original.WithChecked(true);

        // Assert
        Assert.False(original.IsChecked);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var checkbox1 = new Checkbox(ValidId, ValidText, isChecked: true);
        var checkbox2 = new Checkbox(ValidId, ValidText, isChecked: true);

        // Act & Assert
        Assert.Equal(checkbox1, checkbox2);
    }

    [Fact]
    public void Equality_DifferentId_AreNotEqual()
    {
        // Arrange
        var checkbox1 = new Checkbox("id-1", ValidText, isChecked: true);
        var checkbox2 = new Checkbox("id-2", ValidText, isChecked: true);

        // Act & Assert
        Assert.NotEqual(checkbox1, checkbox2);
    }

    [Fact]
    public void Equality_DifferentText_AreNotEqual()
    {
        // Arrange
        var checkbox1 = new Checkbox(ValidId, "Text 1", isChecked: true);
        var checkbox2 = new Checkbox(ValidId, "Text 2", isChecked: true);

        // Act & Assert
        Assert.NotEqual(checkbox1, checkbox2);
    }

    [Fact]
    public void Equality_DifferentCheckedState_AreNotEqual()
    {
        // Arrange
        var checkbox1 = new Checkbox(ValidId, ValidText, isChecked: true);
        var checkbox2 = new Checkbox(ValidId, ValidText, isChecked: false);

        // Act & Assert
        Assert.NotEqual(checkbox1, checkbox2);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WhenUnchecked_ReturnsEmptyBrackets()
    {
        // Arrange
        var checkbox = new Checkbox(ValidId, "My task", isChecked: false);

        // Act & Assert
        Assert.Equal("[ ] My task", checkbox.ToString());
    }

    [Fact]
    public void ToString_WhenChecked_ReturnsXInBrackets()
    {
        // Arrange
        var checkbox = new Checkbox(ValidId, "My task", isChecked: true);

        // Act & Assert
        Assert.Equal("[x] My task", checkbox.ToString());
    }

    #endregion
}
