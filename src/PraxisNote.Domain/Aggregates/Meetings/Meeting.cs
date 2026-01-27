using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.Meetings;

/// <summary>
/// Meeting aggregate - captures meeting metadata and transcript for analysis.
/// </summary>
/// <remarks>
/// Key design decisions:
/// - Title is optional at creation (fire-and-forget workflow for back-to-back meetings)
/// - MeetingDate defaults to now if not specified
/// - Audio is not stored - only transcript and analysis results
/// - Status tracks the processing pipeline: Draft → Processing → Ready → Reviewed
/// </remarks>
public sealed class Meeting : AggregateRoot
{
    /// <summary>
    /// The user who owns this meeting.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// The meeting title. Can be null until user reviews/edits the meeting.
    /// </summary>
    public string? Title { get; private set; }

    /// <summary>
    /// When the meeting occurred.
    /// </summary>
    public DateTimeOffset? MeetingDate { get; private set; }

    /// <summary>
    /// Comma-separated list of attendee names. Optional.
    /// </summary>
    public string? Attendees { get; private set; }

    /// <summary>
    /// Current status in the processing pipeline.
    /// </summary>
    public MeetingStatus Status { get; private set; }

    /// <summary>
    /// When this meeting was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// When this meeting was last modified.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private Meeting() { }

    private Meeting(Guid id, Guid userId, string? title, DateTimeOffset? meetingDate) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));

        var now = DateTimeOffset.UtcNow;

        UserId = userId;
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        MeetingDate = meetingDate ?? now;
        Status = MeetingStatus.Draft;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Creates a new meeting with optional title and date.
    /// </summary>
    /// <remarks>
    /// For back-to-back meetings, title can be added later during review.
    /// If meetingDate is null, defaults to current time.
    /// </remarks>
    public static Meeting Create(Guid userId, string? title = null, DateTimeOffset? meetingDate = null)
    {
        return new Meeting(Guid.NewGuid(), userId, title, meetingDate);
    }

    /// <summary>
    /// Updates the meeting title.
    /// </summary>
    public void UpdateTitle(string? title)
    {
        var newTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();

        if (string.Equals(Title, newTitle, StringComparison.Ordinal))
            return;

        Title = newTitle;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the meeting date.
    /// </summary>
    public void UpdateMeetingDate(DateTimeOffset? meetingDate)
    {
        if (MeetingDate == meetingDate)
            return;

        MeetingDate = meetingDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the attendees list.
    /// </summary>
    public void UpdateAttendees(string? attendees)
    {
        var newAttendees = string.IsNullOrWhiteSpace(attendees) ? null : attendees.Trim();

        if (string.Equals(Attendees, newAttendees, StringComparison.Ordinal))
            return;

        Attendees = newAttendees;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the meeting status.
    /// </summary>
    public void UpdateStatus(MeetingStatus status)
    {
        if (Status == status)
            return;

        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the meeting as reviewed by the user.
    /// </summary>
    public void MarkAsReviewed()
    {
        UpdateStatus(MeetingStatus.Reviewed);
    }
}
