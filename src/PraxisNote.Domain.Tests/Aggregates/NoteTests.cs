using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Domain.Tests.Aggregates;

public class NoteTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly string _validContent = "# My Note\n\nSome content here.";

    #region Create Tests

    [Fact]
    public void Create_WithUserId_CreatesEmptyNote()
    {
        // Act
        var note = Note.Create(_validUserId);

        // Assert
        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.Equal(_validUserId, note.UserId);
        Assert.Equal(string.Empty, note.Content);
        Assert.Empty(note.Checkboxes);
        Assert.Empty(note.LabelIds);
    }

    [Fact]
    public void Create_WithUserIdAndContent_CreatesNoteWithContent()
    {
        // Act
        var note = Note.Create(_validUserId, _validContent);

        // Assert
        Assert.Equal(_validUserId, note.UserId);
        Assert.Equal(_validContent, note.Content);
    }

    [Fact]
    public void Create_WithNullContent_CreatesNoteWithEmptyContent()
    {
        // Act
        var note = Note.Create(_validUserId, null!);

        // Assert
        Assert.Equal(string.Empty, note.Content);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Note.Create(Guid.Empty));
    }

    [Fact]
    public void Create_SetsCreatedAtAndUpdatedAtToSameValue()
    {
        // Act
        var note = Note.Create(_validUserId);

        // Assert
        Assert.Equal(note.CreatedAt, note.UpdatedAt);
    }

    #endregion

    #region UpdateContent Tests

    [Fact]
    public void UpdateContent_WithValidContent_UpdatesContent()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var newContent = "New content";

        // Act
        note.UpdateContent(newContent);

        // Assert
        Assert.Equal(newContent, note.Content);
    }

    [Fact]
    public void UpdateContent_WithNull_SetsEmptyContent()
    {
        // Arrange
        var note = Note.Create(_validUserId, _validContent);

        // Act
        note.UpdateContent(null!);

        // Assert
        Assert.Equal(string.Empty, note.Content);
    }

    [Fact]
    public void UpdateContent_UpdatesUpdatedAt()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var originalUpdatedAt = note.UpdatedAt;

        // Act
        note.UpdateContent("New content");

        // Assert
        Assert.True(note.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateContent_WithSameContent_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var note = Note.Create(_validUserId, _validContent);
        var originalUpdatedAt = note.UpdatedAt;

        // Act
        note.UpdateContent(_validContent);

        // Assert
        Assert.Equal(originalUpdatedAt, note.UpdatedAt);
    }

    #endregion

    #region AddCheckbox Tests

    [Fact]
    public void AddCheckbox_WithValidCheckbox_AddsToCollection()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var checkbox = new Checkbox("cb-1", "My task", isChecked: false);

        // Act
        note.AddCheckbox(checkbox);

        // Assert
        Assert.Single(note.Checkboxes);
        Assert.Contains(checkbox, note.Checkboxes);
    }

    [Fact]
    public void AddCheckbox_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var note = Note.Create(_validUserId);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => note.AddCheckbox(null!));
    }

    [Fact]
    public void AddCheckbox_SameIdTwice_OnlyAddsOnce()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var checkbox1 = new Checkbox("cb-1", "First text", isChecked: false);
        var checkbox2 = new Checkbox("cb-1", "Different text", isChecked: true);

        // Act
        note.AddCheckbox(checkbox1);
        note.AddCheckbox(checkbox2);

        // Assert
        Assert.Single(note.Checkboxes);
        Assert.Equal("First text", note.Checkboxes.First().Text);
    }

    [Fact]
    public void AddCheckbox_UpdatesUpdatedAt()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var originalUpdatedAt = note.UpdatedAt;
        var checkbox = new Checkbox("cb-1", "My task", isChecked: false);

        // Act
        note.AddCheckbox(checkbox);

        // Assert
        Assert.True(note.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void AddCheckbox_DuplicateId_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var checkbox = new Checkbox("cb-1", "My task", isChecked: false);
        note.AddCheckbox(checkbox);
        var updatedAtAfterFirstAdd = note.UpdatedAt;

        // Act
        note.AddCheckbox(new Checkbox("cb-1", "Duplicate", isChecked: true));

        // Assert
        Assert.Equal(updatedAtAfterFirstAdd, note.UpdatedAt);
    }

    [Fact]
    public void AddCheckbox_MultipleCheckboxes_PreservesOrder()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var checkbox1 = new Checkbox("cb-1", "First", isChecked: false);
        var checkbox2 = new Checkbox("cb-2", "Second", isChecked: false);
        var checkbox3 = new Checkbox("cb-3", "Third", isChecked: false);

        // Act
        note.AddCheckbox(checkbox1);
        note.AddCheckbox(checkbox2);
        note.AddCheckbox(checkbox3);

        // Assert
        var checkboxes = note.Checkboxes.ToList();
        Assert.Equal("cb-1", checkboxes[0].Id);
        Assert.Equal("cb-2", checkboxes[1].Id);
        Assert.Equal("cb-3", checkboxes[2].Id);
    }

    #endregion

    #region UpdateCheckbox Tests

    [Fact]
    public void UpdateCheckbox_ExistingCheckbox_UpdatesTextAndState()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        note.AddCheckbox(new Checkbox("cb-1", "Original", isChecked: false));

        // Act
        var result = note.UpdateCheckbox("cb-1", "Updated", isChecked: true);

        // Assert
        Assert.True(result);
        var checkbox = note.GetCheckbox("cb-1");
        Assert.NotNull(checkbox);
        Assert.Equal("Updated", checkbox.Text);
        Assert.True(checkbox.IsChecked);
    }

    [Fact]
    public void UpdateCheckbox_NonExistentCheckbox_ReturnsFalse()
    {
        // Arrange
        var note = Note.Create(_validUserId);

        // Act
        var result = note.UpdateCheckbox("cb-999", "Text", isChecked: false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateCheckbox_UpdatesUpdatedAt()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        note.AddCheckbox(new Checkbox("cb-1", "Original", isChecked: false));
        var originalUpdatedAt = note.UpdatedAt;

        // Act
        note.UpdateCheckbox("cb-1", "Updated", isChecked: true);

        // Assert
        Assert.True(note.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateCheckbox_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        var note = Note.Create(_validUserId);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            note.UpdateCheckbox(null!, "Text", isChecked: false));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateCheckbox_WithEmptyOrWhitespaceId_ThrowsArgumentException(string invalidId)
    {
        // Arrange
        var note = Note.Create(_validUserId);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            note.UpdateCheckbox(invalidId, "Text", isChecked: false));
    }

    [Fact]
    public void UpdateCheckbox_WithNullText_ThrowsArgumentNullException()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        note.AddCheckbox(new Checkbox("cb-1", "Original", isChecked: false));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            note.UpdateCheckbox("cb-1", null!, isChecked: false));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateCheckbox_WithEmptyOrWhitespaceText_ThrowsArgumentException(string invalidText)
    {
        // Arrange
        var note = Note.Create(_validUserId);
        note.AddCheckbox(new Checkbox("cb-1", "Original", isChecked: false));

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            note.UpdateCheckbox("cb-1", invalidText, isChecked: false));
    }

    #endregion

    #region RemoveCheckbox Tests

    [Fact]
    public void RemoveCheckbox_ExistingCheckbox_RemovesFromCollection()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        note.AddCheckbox(new Checkbox("cb-1", "My task", isChecked: false));

        // Act
        var result = note.RemoveCheckbox("cb-1");

        // Assert
        Assert.True(result);
        Assert.Empty(note.Checkboxes);
    }

    [Fact]
    public void RemoveCheckbox_NonExistentCheckbox_ReturnsFalse()
    {
        // Arrange
        var note = Note.Create(_validUserId);

        // Act
        var result = note.RemoveCheckbox("cb-999");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveCheckbox_UpdatesUpdatedAt()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        note.AddCheckbox(new Checkbox("cb-1", "My task", isChecked: false));
        var originalUpdatedAt = note.UpdatedAt;

        // Act
        note.RemoveCheckbox("cb-1");

        // Assert
        Assert.True(note.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void RemoveCheckbox_NonExistent_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var originalUpdatedAt = note.UpdatedAt;

        // Act
        note.RemoveCheckbox("cb-999");

        // Assert
        Assert.Equal(originalUpdatedAt, note.UpdatedAt);
    }

    [Fact]
    public void RemoveCheckbox_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        var note = Note.Create(_validUserId);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => note.RemoveCheckbox(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RemoveCheckbox_WithEmptyOrWhitespaceId_ThrowsArgumentException(string invalidId)
    {
        // Arrange
        var note = Note.Create(_validUserId);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => note.RemoveCheckbox(invalidId));
    }

    #endregion

    #region GetCheckbox Tests

    [Fact]
    public void GetCheckbox_ExistingCheckbox_ReturnsCheckbox()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var checkbox = new Checkbox("cb-1", "My task", isChecked: false);
        note.AddCheckbox(checkbox);

        // Act
        var result = note.GetCheckbox("cb-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(checkbox, result);
    }

    [Fact]
    public void GetCheckbox_NonExistentCheckbox_ReturnsNull()
    {
        // Arrange
        var note = Note.Create(_validUserId);

        // Act
        var result = note.GetCheckbox("cb-999");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region HasCheckbox Tests

    [Fact]
    public void HasCheckbox_ExistingCheckbox_ReturnsTrue()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        note.AddCheckbox(new Checkbox("cb-1", "My task", isChecked: false));

        // Act & Assert
        Assert.True(note.HasCheckbox("cb-1"));
    }

    [Fact]
    public void HasCheckbox_NonExistentCheckbox_ReturnsFalse()
    {
        // Arrange
        var note = Note.Create(_validUserId);

        // Act & Assert
        Assert.False(note.HasCheckbox("cb-999"));
    }

    #endregion

    #region Label Tests

    [Fact]
    public void AddLabel_WithValidLabelId_AddsToLabelIds()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var labelId = Guid.NewGuid();

        // Act
        note.AddLabel(labelId);

        // Assert
        Assert.Contains(labelId, note.LabelIds);
        Assert.Single(note.LabelIds);
    }

    [Fact]
    public void AddLabel_WithEmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var note = Note.Create(_validUserId);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => note.AddLabel(Guid.Empty));
    }

    [Fact]
    public void AddLabel_SameLabelTwice_OnlyAddsOnce()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var labelId = Guid.NewGuid();

        // Act
        note.AddLabel(labelId);
        note.AddLabel(labelId);

        // Assert
        Assert.Single(note.LabelIds);
    }

    [Fact]
    public void AddLabel_UpdatesUpdatedAt()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var originalUpdatedAt = note.UpdatedAt;

        // Act
        note.AddLabel(Guid.NewGuid());

        // Assert
        Assert.True(note.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void RemoveLabel_ExistingLabel_RemovesFromLabelIds()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var labelId = Guid.NewGuid();
        note.AddLabel(labelId);

        // Act
        note.RemoveLabel(labelId);

        // Assert
        Assert.DoesNotContain(labelId, note.LabelIds);
        Assert.Empty(note.LabelIds);
    }

    [Fact]
    public void RemoveLabel_NonExistentLabel_DoesNotThrow()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var originalUpdatedAt = note.UpdatedAt;

        // Act - should not throw
        note.RemoveLabel(Guid.NewGuid());

        // Assert - UpdatedAt should not change since nothing was removed
        Assert.Equal(originalUpdatedAt, note.UpdatedAt);
    }

    [Fact]
    public void HasLabel_WhenLabelExists_ReturnsTrue()
    {
        // Arrange
        var note = Note.Create(_validUserId);
        var labelId = Guid.NewGuid();
        note.AddLabel(labelId);

        // Act & Assert
        Assert.True(note.HasLabel(labelId));
    }

    [Fact]
    public void HasLabel_WhenLabelDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var note = Note.Create(_validUserId);

        // Act & Assert
        Assert.False(note.HasLabel(Guid.NewGuid()));
    }

    [Fact]
    public void Create_HasEmptyLabelIds()
    {
        // Act
        var note = Note.Create(_validUserId);

        // Assert
        Assert.Empty(note.LabelIds);
    }

    #endregion
}
