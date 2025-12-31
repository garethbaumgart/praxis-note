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
/// - Labels stored as IDs only (aggregates don't reference other aggregates)
/// </remarks>
public sealed class TaskItem : AggregateRoot
{
    private readonly List<Guid> _labelIds = [];

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
    /// Optional due date for the task.
    /// </summary>
    public DueDate? DueDate { get; private set; }

    /// <summary>
    /// Reference to the source note checkbox, if this task was created from a note.
    /// Null for standalone tasks created directly on the board.
    /// </summary>
    public CheckboxRef? CheckboxRef { get; private init; }

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
    /// IDs of labels assigned to this task.
    /// </summary>
    /// <remarks>
    /// Stored as IDs only - aggregates don't hold references to other aggregates.
    /// The application layer joins with Label entities for display.
    /// </remarks>
    public IReadOnlyCollection<Guid> LabelIds => _labelIds.AsReadOnly();

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private TaskItem() { }

    private TaskItem(
        Guid id,
        Guid userId,
        string title,
        CheckboxRef? checkboxRef) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var now = DateTimeOffset.UtcNow;

        UserId = userId;
        Title = title.Trim();
        Status = TaskStatus.Todo;
        CheckboxRef = checkboxRef;
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
    /// Moves the task to InProgress status.
    /// </summary>
    /// <remarks>
    /// Sets StartedAt if not already set.
    /// </remarks>
    public void Start()
    {
        var now = DateTimeOffset.UtcNow;

        Status = TaskStatus.InProgress;
        StartedAt ??= now;  // Only set if not already started before
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
        CompletedAt ??= now;  // Preserve first completion time
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
    /// Adds a label to this task.
    /// </summary>
    /// <param name="labelId">The ID of the label to add.</param>
    /// <remarks>
    /// Idempotent - adding the same label twice has no effect.
    /// Does not validate that the label exists or belongs to the same user.
    /// That validation should happen in the application layer.
    /// </remarks>
    public void AddLabel(Guid labelId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(labelId, Guid.Empty, nameof(labelId));

        if (_labelIds.Contains(labelId))
            return;

        _labelIds.Add(labelId);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Removes a label from this task.
    /// </summary>
    /// <param name="labelId">The ID of the label to remove.</param>
    /// <remarks>
    /// Idempotent - removing a non-existent label has no effect.
    /// </remarks>
    public void RemoveLabel(Guid labelId)
    {
        if (_labelIds.Remove(labelId))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Returns true if this task has the specified label.
    /// </summary>
    public bool HasLabel(Guid labelId) => _labelIds.Contains(labelId);

    /// <summary>
    /// Returns true if this task was created from a note checkbox.
    /// </summary>
    public bool IsLinkedToNote => CheckboxRef?.IsLinked ?? false;
}
