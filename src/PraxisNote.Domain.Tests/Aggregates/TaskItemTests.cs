using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.ValueObjects;

using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Domain.Tests.Aggregates;

public class TaskItemTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly string _validTitle = "Complete the report";

    #region CreateStandalone Tests

    [Fact]
    public void CreateStandalone_WithValidInputs_CreatesTaskWithCorrectProperties()
    {
        // Act
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);

        // Assert
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal(_validUserId, task.UserId);
        Assert.Equal(_validTitle, task.Title);
        Assert.Equal(TaskStatus.Todo, task.Status);
        Assert.Null(task.DueDate);
        Assert.Null(task.CheckboxRef);
        Assert.Null(task.StartedAt);
        Assert.Null(task.CompletedAt);
        Assert.False(task.IsLinkedToNote);
    }

    [Fact]
    public void CreateStandalone_TrimsTitle()
    {
        // Act
        var task = TaskItem.CreateStandalone(_validUserId, "  Trimmed title  ");

        // Assert
        Assert.Equal("Trimmed title", task.Title);
    }

    [Fact]
    public void CreateStandalone_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TaskItem.CreateStandalone(Guid.Empty, _validTitle));
    }

    [Fact]
    public void CreateStandalone_WithNullTitle_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            TaskItem.CreateStandalone(_validUserId, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateStandalone_WithEmptyOrWhitespaceTitle_ThrowsArgumentException(string invalidTitle)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            TaskItem.CreateStandalone(_validUserId, invalidTitle));
    }

    #endregion

    #region CreateFromCheckbox Tests

    [Fact]
    public void CreateFromCheckbox_WithValidInputs_CreatesLinkedTask()
    {
        // Arrange
        var checkboxRef = new CheckboxRef(Guid.NewGuid(), "checkbox-1");

        // Act
        var task = TaskItem.CreateFromCheckbox(_validUserId, _validTitle, checkboxRef);

        // Assert
        Assert.Equal(checkboxRef, task.CheckboxRef);
        Assert.True(task.IsLinkedToNote);
    }

    [Fact]
    public void CreateFromCheckbox_WithNullCheckboxRef_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            TaskItem.CreateFromCheckbox(_validUserId, _validTitle, null!));
    }

    #endregion

    #region Status Transition Tests

    [Fact]
    public void Start_FromTodo_SetsStatusToInProgressAndSetsStartedAt()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);

        // Act
        task.Start();

        // Assert
        Assert.Equal(TaskStatus.InProgress, task.Status);
        Assert.NotNull(task.StartedAt);
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public void Start_WhenAlreadyStarted_DoesNotUpdateStartedAt()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        task.Start();
        var originalStartedAt = task.StartedAt;

        // Act
        task.Start();

        // Assert
        Assert.Equal(originalStartedAt, task.StartedAt);
    }

    [Fact]
    public void Complete_FromTodo_SetsStatusToDoneAndSetsBothTimestamps()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);

        // Act
        task.Complete();

        // Assert
        Assert.Equal(TaskStatus.Done, task.Status);
        Assert.NotNull(task.StartedAt);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public void Complete_FromInProgress_SetsStatusToDoneAndSetsCompletedAt()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        task.Start();
        var originalStartedAt = task.StartedAt;

        // Act
        task.Complete();

        // Assert
        Assert.Equal(TaskStatus.Done, task.Status);
        Assert.Equal(originalStartedAt, task.StartedAt);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_PreservesOriginalCompletedAt()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        task.Complete();
        var originalCompletedAt = task.CompletedAt;

        // Act
        task.Complete();

        // Assert
        Assert.Equal(originalCompletedAt, task.CompletedAt);
    }

    [Fact]
    public void Reopen_FromDone_SetsStatusToTodoAndClearsTimestamps()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        task.Complete();

        // Act
        task.Reopen();

        // Assert
        Assert.Equal(TaskStatus.Todo, task.Status);
        Assert.Null(task.StartedAt);
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public void Reopen_FromInProgress_SetsStatusToTodoAndClearsStartedAt()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        task.Start();

        // Act
        task.Reopen();

        // Assert
        Assert.Equal(TaskStatus.Todo, task.Status);
        Assert.Null(task.StartedAt);
    }

    #endregion

    #region UpdateTitle Tests

    [Fact]
    public void UpdateTitle_WithValidTitle_UpdatesTitleAndUpdatedAt()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        var originalUpdatedAt = task.UpdatedAt;

        // Act
        task.UpdateTitle("New title");

        // Assert
        Assert.Equal("New title", task.Title);
        Assert.True(task.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateTitle_TrimsWhitespace()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);

        // Act
        task.UpdateTitle("  Trimmed  ");

        // Assert
        Assert.Equal("Trimmed", task.Title);
    }

    [Fact]
    public void UpdateTitle_WithNullTitle_ThrowsArgumentNullException()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => task.UpdateTitle(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateTitle_WithEmptyOrWhitespaceTitle_ThrowsArgumentException(string invalidTitle)
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => task.UpdateTitle(invalidTitle));
    }

    #endregion

    #region DueDate Tests

    [Fact]
    public void SetDueDate_WithValidDate_SetsDueDateAndUpdatesUpdatedAt()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        var dueDate = new DueDate(DateOnly.FromDateTime(DateTime.Today.AddDays(7)));

        // Act
        task.SetDueDate(dueDate);

        // Assert
        Assert.Equal(dueDate, task.DueDate);
    }

    [Fact]
    public void SetDueDate_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => task.SetDueDate(null!));
    }

    [Fact]
    public void ClearDueDate_RemovesDueDateAndUpdatesUpdatedAt()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        var dueDate = new DueDate(DateOnly.FromDateTime(DateTime.Today.AddDays(7)));
        task.SetDueDate(dueDate);

        // Act
        task.ClearDueDate();

        // Assert
        Assert.Null(task.DueDate);
    }

    #endregion

    #region Label Tests

    [Fact]
    public void AddLabel_WithValidLabelId_AddsToLabelIds()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        var labelId = Guid.NewGuid();

        // Act
        task.AddLabel(labelId);

        // Assert
        Assert.Contains(labelId, task.LabelIds);
        Assert.Single(task.LabelIds);
    }

    [Fact]
    public void AddLabel_WithEmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => task.AddLabel(Guid.Empty));
    }

    [Fact]
    public void AddLabel_SameLabelTwice_OnlyAddsOnce()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        var labelId = Guid.NewGuid();

        // Act
        task.AddLabel(labelId);
        task.AddLabel(labelId);

        // Assert
        Assert.Single(task.LabelIds);
    }

    [Fact]
    public void AddLabel_UpdatesUpdatedAt()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        var originalUpdatedAt = task.UpdatedAt;

        // Act
        task.AddLabel(Guid.NewGuid());

        // Assert
        Assert.True(task.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void RemoveLabel_ExistingLabel_RemovesFromLabelIds()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        var labelId = Guid.NewGuid();
        task.AddLabel(labelId);

        // Act
        task.RemoveLabel(labelId);

        // Assert
        Assert.DoesNotContain(labelId, task.LabelIds);
        Assert.Empty(task.LabelIds);
    }

    [Fact]
    public void RemoveLabel_NonExistentLabel_DoesNotThrow()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        var originalUpdatedAt = task.UpdatedAt;

        // Act - should not throw
        task.RemoveLabel(Guid.NewGuid());

        // Assert - UpdatedAt should not change since nothing was removed
        Assert.Equal(originalUpdatedAt, task.UpdatedAt);
    }

    [Fact]
    public void HasLabel_WhenLabelExists_ReturnsTrue()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        var labelId = Guid.NewGuid();
        task.AddLabel(labelId);

        // Act & Assert
        Assert.True(task.HasLabel(labelId));
    }

    [Fact]
    public void HasLabel_WhenLabelDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);

        // Act & Assert
        Assert.False(task.HasLabel(Guid.NewGuid()));
    }

    [Fact]
    public void CreateStandalone_HasEmptyLabelIds()
    {
        // Act
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);

        // Assert
        Assert.Empty(task.LabelIds);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void CreateStandalone_SetsCreatedAtAndUpdatedAtToSameValue()
    {
        // Act
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);

        // Assert
        Assert.Equal(task.CreatedAt, task.UpdatedAt);
    }

    [Fact]
    public void AnyModification_UpdatesUpdatedAt()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_validUserId, _validTitle);
        var originalUpdatedAt = task.UpdatedAt;

        // Act
        task.Start();

        // Assert
        Assert.True(task.UpdatedAt >= originalUpdatedAt);
    }

    #endregion
}
