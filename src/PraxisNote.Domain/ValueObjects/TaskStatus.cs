namespace PraxisNote.Domain.ValueObjects;

/// <summary>
/// Represents the lifecycle status of a task.
/// All transitions between states are allowed.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Task has not been started.
    /// </summary>
    Todo,

    /// <summary>
    /// Task is actively being worked on.
    /// </summary>
    InProgress,

    /// <summary>
    /// Task has been completed.
    /// </summary>
    Done
}
