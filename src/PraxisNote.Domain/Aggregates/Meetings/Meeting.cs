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
    private readonly HashSet<Guid> _tagIds = [];
    private readonly List<ActionItem> _actionItems = [];

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
    /// The meeting transcript content. Optional - can be pasted or generated from audio.
    /// </summary>
    public string? TranscriptContent { get; private set; }

    /// <summary>
    /// AI-generated summary of the meeting.
    /// </summary>
    public string? Summary { get; private set; }

    /// <summary>
    /// AI-generated key discussion points. Stored as JSON array.
    /// </summary>
    public string? KeyPoints { get; private set; }

    /// <summary>
    /// AI-generated decisions made during the meeting. Stored as JSON array.
    /// </summary>
    public string? Decisions { get; private set; }

    /// <summary>
    /// AI-generated behavioral analysis. Stored as JSON object containing:
    /// - speakingDynamics: talk time, interruptions, question ratios per participant
    /// - sentimentTone: sentiment scores, tone shifts, emotional indicators
    /// - communicationPatterns: clarity, follow-ups, engagement levels
    /// - redFlags: evasive language, hedging, defensive responses
    /// </summary>
    public string? BehavioralAnalysis { get; private set; }

    /// <summary>
    /// Tag IDs associated with this meeting.
    /// </summary>
    public IReadOnlyCollection<Guid> TagIds => _tagIds.AsReadOnly();

    /// <summary>
    /// AI-extracted action items from the meeting transcript.
    /// </summary>
    public IReadOnlyCollection<ActionItem> ActionItems => _actionItems.AsReadOnly();

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

    private Meeting(Guid id, Guid userId, string? title, DateTimeOffset? meetingDate, string? attendees) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));

        var now = DateTimeOffset.UtcNow;

        UserId = userId;
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        MeetingDate = meetingDate ?? now;
        Attendees = string.IsNullOrWhiteSpace(attendees) ? null : attendees.Trim();
        Status = MeetingStatus.Draft;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Creates a new meeting with optional title, date, and attendees.
    /// </summary>
    /// <remarks>
    /// For back-to-back meetings, title can be added later during review.
    /// If meetingDate is null, defaults to current time.
    /// </remarks>
    public static Meeting Create(Guid userId, string? title = null, DateTimeOffset? meetingDate = null, string? attendees = null)
    {
        return new Meeting(Guid.NewGuid(), userId, title, meetingDate, attendees);
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

    /// <summary>
    /// Submits a transcript for this meeting.
    /// </summary>
    /// <remarks>
    /// The transcript can be pasted manually or generated from audio.
    /// Status remains Draft after submission - changes to Processing when AI analyzes.
    /// </remarks>
    public void SubmitTranscript(string transcript)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript, nameof(transcript));

        TranscriptContent = transcript.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Clears the transcript content.
    /// </summary>
    public void ClearTranscript()
    {
        if (TranscriptContent is null)
            return;

        TranscriptContent = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the meeting as processing for transcription.
    /// </summary>
    public void StartTranscription()
    {
        UpdateStatus(MeetingStatus.Processing);
    }

    /// <summary>
    /// Completes transcription by storing the transcript text and resetting status to Draft.
    /// </summary>
    /// <param name="transcript">The transcribed text from audio.</param>
    public void CompleteTranscription(string transcript)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript, nameof(transcript));

        TranscriptContent = transcript.Trim();
        UpdateStatus(MeetingStatus.Draft);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Starts AI analysis of the meeting transcript.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no transcript exists.</exception>
    public void StartAnalysis()
    {
        if (string.IsNullOrWhiteSpace(TranscriptContent))
            throw new InvalidOperationException("Cannot analyze meeting without transcript");

        UpdateStatus(MeetingStatus.Processing);
    }

    /// <summary>
    /// Completes AI analysis with the generated results.
    /// </summary>
    /// <param name="summary">The AI-generated summary.</param>
    /// <param name="keyPoints">JSON array of key discussion points.</param>
    /// <param name="decisions">JSON array of decisions made.</param>
    /// <param name="behavioralAnalysis">JSON object with behavioral analysis data.</param>
    /// <param name="actionItems">Extracted action items from the transcript.</param>
    public void CompleteAnalysis(
        string summary,
        string? keyPoints,
        string? decisions,
        string? behavioralAnalysis = null,
        IEnumerable<ActionItem>? actionItems = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary, nameof(summary));

        Summary = summary.Trim();
        KeyPoints = keyPoints;
        Decisions = decisions;
        BehavioralAnalysis = behavioralAnalysis;

        _actionItems.Clear();
        if (actionItems is not null)
        {
            _actionItems.AddRange(actionItems);
        }

        UpdateStatus(MeetingStatus.Ready);
    }

    /// <summary>
    /// Marks the analysis as failed.
    /// </summary>
    public void FailAnalysis()
    {
        UpdateStatus(MeetingStatus.Failed);
    }

    /// <summary>
    /// Clears the analysis results and resets status to Draft.
    /// </summary>
    public void ClearAnalysis()
    {
        if (Summary is null && KeyPoints is null && Decisions is null && BehavioralAnalysis is null && _actionItems.Count == 0 && Status != MeetingStatus.Failed)
            return;

        Summary = null;
        KeyPoints = null;
        Decisions = null;
        BehavioralAnalysis = null;
        _actionItems.Clear();
        UpdateStatus(MeetingStatus.Draft);
    }

    #region Tag Management

    /// <summary>
    /// Adds a tag to this meeting. Idempotent - adding an existing tag is a no-op.
    /// </summary>
    public void AddTag(Guid tagId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tagId, Guid.Empty, nameof(tagId));

        if (_tagIds.Add(tagId))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Removes a tag from this meeting. Idempotent - removing a non-existent tag is a no-op.
    /// </summary>
    public void RemoveTag(Guid tagId)
    {
        if (_tagIds.Remove(tagId))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Checks if this meeting has the specified tag.
    /// </summary>
    public bool HasTag(Guid tagId) => _tagIds.Contains(tagId);

    #endregion

    #region Action Item Management

    /// <summary>
    /// Toggles the completion status of an action item.
    /// </summary>
    /// <returns>True if the item was found and toggled, false otherwise.</returns>
    public bool ToggleActionItem(Guid actionItemId)
    {
        var index = _actionItems.FindIndex(a => a.Id == actionItemId);
        if (index < 0)
            return false;

        _actionItems[index] = _actionItems[index].WithCompletedToggled();
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    /// <summary>
    /// Gets an action item by ID.
    /// </summary>
    /// <returns>The action item if found, null otherwise.</returns>
    public ActionItem? GetActionItem(Guid actionItemId) =>
        _actionItems.Find(a => a.Id == actionItemId);

    #endregion
}
