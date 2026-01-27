namespace PraxisNote.Domain.Aggregates.Meetings;

/// <summary>
/// Represents the lifecycle status of a meeting.
/// </summary>
public enum MeetingStatus
{
    /// <summary>
    /// Meeting has been created but not yet processed (no transcript).
    /// </summary>
    Draft,

    /// <summary>
    /// Meeting is being transcribed or analyzed.
    /// </summary>
    Processing,

    /// <summary>
    /// Meeting has been processed and is ready for review.
    /// </summary>
    Ready,

    /// <summary>
    /// User has reviewed the meeting.
    /// </summary>
    Reviewed,

    /// <summary>
    /// Processing failed (transcription or analysis error).
    /// </summary>
    Failed
}
