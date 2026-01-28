using PraxisNote.Domain.Common;
using PraxisNote.Domain.ValueObjects;

using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Domain.Aggregates.Tasks;

/// <summary>
/// Task aggregate - represents an actionable item with a three-state lifecycle.
/// Named TaskItem to avoid conflict with System.Threading.Tasks.Task.
/// </summary>
/// <remarks>
/// Key design decisions:
/// - Status changes update relevant timestamps (StartedAt, CompletedAt)
/// - UpdatedAt is modified on any state change
/// - Tags stored as IDs only via TagIds property (aggregates don't reference other aggregates)
/// </remarks>
public sealed class TaskItem : AggregateRoot
{
    private readonly HashSet<Guid> _tagIds = [];
    private readonly List<Comment> _comments = [];

    /// <summary>
    /// The user who owns this task.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// The task title/description.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Current status in the task lifecycle.
    /// </summary>
    public TaskStatus Status { get; private set; }

    /// <summary>
    /// Position within the status column for ordering.
    /// Lower values appear first. New tasks start at position 0;
    /// existing tasks in the same status column have their positions
    /// incremented to maintain relative ordering.
    /// </summary>
    public int Position { get; private set; } = 0;

    /// <summary>
    /// Optional due date for the task.
    /// </summary>
    public DueDate? DueDate { get; private set; }

    /// <summary>
    /// Whether this task is marked as high priority.
    /// </summary>
    public bool IsPriority { get; private set; }

    /// <summary>
    /// Reference to the source note checkbox, if this task was created from a note.
    /// Null for standalone tasks created directly on the board.
    /// </summary>
    public CheckboxRef? CheckboxRef { get; private init; }

    /// <summary>
    /// Reference to the source meeting action item, if this task was created from a meeting.
    /// Null for standalone tasks or tasks created from notes.
    /// </summary>
    public ActionItemRef? ActionItemRef { get; private init; }

    /// <summary>
    /// When this task was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// When this task was last modified.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// When the task was moved to InProgress. Null if never started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>
    /// When the task was completed. Null if not done.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// IDs of tags assigned to this task.
    /// </summary>
    /// <remarks>
    /// Stored as IDs only - aggregates don't hold references to other aggregates.
    /// The application layer joins with Tag entities for display.
    /// </remarks>
    public IReadOnlyCollection<Guid> TagIds => _tagIds.AsReadOnly();

    /// <summary>
    /// Comments on this task for tracking progress.
    /// Stored as JSONB array in the database.
    /// </summary>
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private TaskItem() { }

    private TaskItem(
        Guid id,
        Guid userId,
        string title,
        CheckboxRef? checkboxRef,
        ActionItemRef? actionItemRef = null) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var now = DateTimeOffset.UtcNow;

        UserId = userId;
        Title = title.Trim();
        Status = TaskStatus.Todo;
        CheckboxRef = checkboxRef;
        ActionItemRef = actionItemRef;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Creates a standalone task directly on the board.
    /// </summary>
    public static TaskItem CreateStandalone(Guid userId, string title)
    {
        return new TaskItem(Guid.NewGuid(), userId, title, checkboxRef: null);
    }

    /// <summary>
    /// Creates a task from a note's checkbox.
    /// </summary>
    public static TaskItem CreateFromCheckbox(Guid userId, string title, CheckboxRef checkboxRef)
    {
        ArgumentNullException.ThrowIfNull(checkboxRef);
        return new TaskItem(Guid.NewGuid(), userId, title, checkboxRef);
    }

    /// <summary>
    /// Creates a task from a meeting's action item.
    /// </summary>
    public static TaskItem CreateFromActionItem(Guid userId, string title, ActionItemRef actionItemRef)
    {
        ArgumentNullException.ThrowIfNull(actionItemRef);
        return new TaskItem(Guid.NewGuid(), userId, title, checkboxRef: null, actionItemRef);
    }

    /// <summary>
    /// Moves the task to InProgress status.
    /// </summary>
    /// <remarks>
    /// Sets StartedAt only if not already set, to track when work first began.
    /// </remarks>
    public void Start()
    {
        var now = DateTimeOffset.UtcNow;

        Status = TaskStatus.InProgress;
        StartedAt ??= now;  // Only set if not already started
        CompletedAt = null;  // Clear any previous completion
        UpdatedAt = now;
    }

    /// <summary>
    /// Moves the task to Done status.
    /// </summary>
    /// <remarks>
    /// Can be called from any status. Sets CompletedAt.
    /// If coming from Todo, also sets StartedAt (task was implicitly started).
    /// </remarks>
    public void Complete()
    {
        var now = DateTimeOffset.UtcNow;

        Status = TaskStatus.Done;
        StartedAt ??= now;  // If completing from Todo, mark as started too
        CompletedAt = now;  // Always set to track most recent completion
        UpdatedAt = now;
    }

    /// <summary>
    /// Moves the task back to Todo status.
    /// </summary>
    /// <remarks>
    /// Clears StartedAt and CompletedAt timestamps.
    /// </remarks>
    public void Reopen()
    {
        var now = DateTimeOffset.UtcNow;

        Status = TaskStatus.Todo;
        StartedAt = null;
        CompletedAt = null;
        UpdatedAt = now;
    }

    /// <summary>
    /// Updates the task title.
    /// </summary>
    public void UpdateTitle(string newTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newTitle);

        Title = newTitle.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Sets or updates the due date.
    /// </summary>
    public void SetDueDate(DueDate dueDate)
    {
        ArgumentNullException.ThrowIfNull(dueDate);

        DueDate = dueDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Removes the due date.
    /// </summary>
    public void ClearDueDate()
    {
        DueDate = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Toggles the priority flag.
    /// </summary>
    public void TogglePriority()
    {
        IsPriority = !IsPriority;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the task's position within its status column.
    /// </summary>
    public void SetPosition(int position)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        Position = position;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds a tag to this task.
    /// </summary>
    /// <param name="tagId">The ID of the tag to add.</param>
    /// <remarks>
    /// Idempotent - adding the same tag twice has no effect.
    /// Does not validate that the tag exists or belongs to the same user.
    /// That validation should happen in the application layer.
    /// </remarks>
    public void AddTag(Guid tagId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tagId, Guid.Empty, nameof(tagId));

        if (_tagIds.Add(tagId))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Removes a tag from this task.
    /// </summary>
    /// <param name="tagId">The ID of the tag to remove.</param>
    /// <remarks>
    /// Idempotent - removing a non-existent tag has no effect.
    /// </remarks>
    public void RemoveTag(Guid tagId)
    {
        if (_tagIds.Remove(tagId))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Returns true if this task has the specified tag.
    /// </summary>
    public bool HasTag(Guid tagId) => _tagIds.Contains(tagId);

    /// <summary>
    /// Returns true if this task was created from a note checkbox.
    /// </summary>
    public bool IsLinkedToNote => CheckboxRef?.IsLinked ?? false;

    /// <summary>
    /// Returns true if this task was created from a meeting action item.
    /// </summary>
    public bool IsLinkedToMeeting => ActionItemRef?.IsLinked ?? false;

    /// <summary>
    /// Adds a comment to this task.
    /// </summary>
    /// <param name="content">The comment text.</param>
    /// <returns>The created comment.</returns>
    public Comment AddComment(string content)
    {
        var comment = Comment.Create(content);
        _comments.Add(comment);
        UpdatedAt = DateTimeOffset.UtcNow;
        return comment;
    }

    /// <summary>
    /// Updates an existing comment's content.
    /// </summary>
    /// <param name="commentId">The ID of the comment to update.</param>
    /// <param name="newContent">The new content.</param>
    /// <returns>The updated comment, or null if not found.</returns>
    public Comment? UpdateComment(Guid commentId, string newContent)
    {
        var index = _comments.FindIndex(c => c.Id == commentId);
        if (index < 0)
        {
            return null;
        }

        var updated = _comments[index].WithUpdatedContent(newContent);
        _comments[index] = updated;
        UpdatedAt = DateTimeOffset.UtcNow;
        return updated;
    }

    /// <summary>
    /// Removes a comment from this task.
    /// </summary>
    /// <param name="commentId">The ID of the comment to remove.</param>
    /// <returns>True if the comment was removed, false if not found.</returns>
    public bool RemoveComment(Guid commentId)
    {
        var removed = _comments.RemoveAll(c => c.Id == commentId) > 0;
        if (removed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
        return removed;
    }

    /// <summary>
    /// Gets a comment by ID.
    /// </summary>
    public Comment? GetComment(Guid commentId) => _comments.Find(c => c.Id == commentId);
}
