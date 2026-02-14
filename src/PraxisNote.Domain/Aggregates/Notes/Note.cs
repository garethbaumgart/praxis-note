using PraxisNote.Domain.Common;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Domain.Aggregates.Notes;

/// <summary>
/// Note aggregate - a rich text container with embedded checkboxes that become tasks.
/// </summary>
/// <remarks>
/// Key design decisions:
/// - Content is stored as an opaque string (format-agnostic: TipTap, ProseMirror, Markdown, etc.)
/// - Checkboxes are maintained as a separate collection, synced by the application layer
/// - Tags stored as IDs only (aggregates don't reference other aggregates)
/// - Editor plugin choice is deferred to infrastructure layer
/// </remarks>
public sealed class Note : AggregateRoot
{
    private readonly List<Checkbox> _checkboxes = [];
    private readonly HashSet<Guid> _tagIds = [];

    /// <summary>
    /// The user who owns this note.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The profile this note belongs to (data silo boundary).
    /// </summary>
    public Guid ProfileId { get; private set; }

    /// <summary>
    /// The raw note content. Format depends on the editor plugin (TipTap JSON, Markdown, etc.).
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Checkboxes extracted from the content. Managed by the application layer via ICheckboxExtractor.
    /// </summary>
    public IReadOnlyCollection<Checkbox> Checkboxes => _checkboxes.AsReadOnly();

    /// <summary>
    /// IDs of tags assigned to this note.
    /// </summary>
    /// <remarks>
    /// When a checkbox becomes a task, the task inherits these tags at creation time.
    /// </remarks>
    public IReadOnlyCollection<Guid> TagIds => _tagIds.AsReadOnly();

    /// <summary>
    /// When this note was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// When this note was last modified.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private Note() { }

    private Note(Guid id, Guid userId, Guid profileId, string content) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty, nameof(profileId));

        var now = DateTimeOffset.UtcNow;

        UserId = userId;
        ProfileId = profileId;
        Content = content ?? string.Empty;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Creates a new empty note.
    /// </summary>
    public static Note Create(Guid userId, Guid profileId)
    {
        return new Note(Guid.NewGuid(), userId, profileId, string.Empty);
    }

    /// <summary>
    /// Creates a new note with initial content.
    /// </summary>
    public static Note Create(Guid userId, Guid profileId, string content)
    {
        return new Note(Guid.NewGuid(), userId, profileId, content);
    }

    #region Content Management

    /// <summary>
    /// Updates the note content.
    /// </summary>
    /// <remarks>
    /// After updating content, the application layer should parse the new content
    /// and call checkbox management methods to sync the checkbox collection.
    /// </remarks>
    public void UpdateContent(string content)
    {
        var newContent = content ?? string.Empty;

        if (string.Equals(Content, newContent, StringComparison.Ordinal))
            return;

        Content = newContent;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    #endregion

    #region Checkbox Management

    /// <summary>
    /// Adds a checkbox to this note.
    /// </summary>
    /// <remarks>
    /// Idempotent - if a checkbox with the same ID exists, this is a no-op.
    /// </remarks>
    public void AddCheckbox(Checkbox checkbox)
    {
        ArgumentNullException.ThrowIfNull(checkbox);

        if (_checkboxes.Any(c => c.Id == checkbox.Id))
            return;

        _checkboxes.Add(checkbox);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates an existing checkbox's text and checked state.
    /// </summary>
    /// <returns>True if the checkbox was found and updated, false otherwise.</returns>
    public bool UpdateCheckbox(string checkboxId, string text, bool isChecked)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkboxId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var index = _checkboxes.FindIndex(c => c.Id == checkboxId);
        if (index < 0)
            return false;

        _checkboxes[index] = new Checkbox(checkboxId, text, isChecked);
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    /// <summary>
    /// Removes a checkbox from this note.
    /// </summary>
    /// <remarks>
    /// Idempotent - if the checkbox doesn't exist, this is a no-op.
    /// </remarks>
    /// <returns>True if the checkbox was found and removed, false otherwise.</returns>
    public bool RemoveCheckbox(string checkboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkboxId);

        var removed = _checkboxes.RemoveAll(c => c.Id == checkboxId) > 0;
        if (removed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
        return removed;
    }

    /// <summary>
    /// Gets a checkbox by its ID.
    /// </summary>
    /// <returns>The checkbox if found, null otherwise.</returns>
    public Checkbox? GetCheckbox(string checkboxId)
    {
        return _checkboxes.FirstOrDefault(c => c.Id == checkboxId);
    }

    /// <summary>
    /// Returns true if this note contains a checkbox with the specified ID.
    /// </summary>
    public bool HasCheckbox(string checkboxId) => _checkboxes.Any(c => c.Id == checkboxId);

    #endregion

    #region Tag Management

    /// <summary>
    /// Adds a tag to this note.
    /// </summary>
    /// <remarks>
    /// Idempotent - adding the same tag twice has no effect.
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
    /// Removes a tag from this note.
    /// </summary>
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
    /// Returns true if this note has the specified tag.
    /// </summary>
    public bool HasTag(Guid tagId) => _tagIds.Contains(tagId);

    #endregion

    /// <summary>
    /// Reassigns this note to a different user and profile.
    /// Used during account linking to transfer data before deleting the source user.
    /// </summary>
    public void Reassign(Guid newUserId, Guid newProfileId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(newUserId, Guid.Empty, nameof(newUserId));
        ArgumentOutOfRangeException.ThrowIfEqual(newProfileId, Guid.Empty, nameof(newProfileId));

        UserId = newUserId;
        ProfileId = newProfileId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
