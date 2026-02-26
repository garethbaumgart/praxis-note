using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.DriveFileImports;

/// <summary>
/// Tracks a file discovered in a connected Google Drive folder.
/// Status machine: Pending -> Parsed -> Imported (or Skipped/Error from most states).
/// </summary>
public sealed class DriveFileImport : AggregateRoot
{
    /// <summary>
    /// The Drive connection this file was discovered from.
    /// </summary>
    public Guid DriveConnectionId { get; private set; }

    /// <summary>
    /// The Google Drive file ID (unique within Google Drive).
    /// </summary>
    public string DriveFileId { get; private init; } = null!;

    /// <summary>
    /// The file name as reported by Google Drive.
    /// </summary>
    public string FileName { get; private set; } = null!;

    /// <summary>
    /// The MIME type of the file (e.g., text/plain, application/vnd.google-apps.document).
    /// </summary>
    public string MimeType { get; private set; } = null!;

    /// <summary>
    /// When the file was last modified in Google Drive.
    /// </summary>
    public DateTimeOffset FileModifiedTime { get; private set; }

    /// <summary>
    /// Current import status in the processing pipeline.
    /// </summary>
    public DriveFileImportStatus Status { get; private set; }

    /// <summary>
    /// The meeting this file was matched/imported to, if applicable.
    /// </summary>
    public Guid? MatchedMeetingId { get; private set; }

    /// <summary>
    /// The extracted text content from the file, set during parsing.
    /// </summary>
    public string? ParsedContent { get; private set; }

    /// <summary>
    /// When the file was successfully parsed.
    /// </summary>
    public DateTimeOffset? ParsedAt { get; private set; }

    /// <summary>
    /// When the file was imported into a meeting.
    /// </summary>
    public DateTimeOffset? ImportedAt { get; private set; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// When the file was first discovered during a sync.
    /// </summary>
    public DateTimeOffset DiscoveredAt { get; private init; }

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private DriveFileImport() { }

    private DriveFileImport(
        Guid id,
        Guid driveConnectionId,
        string driveFileId,
        string fileName,
        string mimeType,
        DateTimeOffset fileModifiedTime) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(driveConnectionId, Guid.Empty, nameof(driveConnectionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(driveFileId, nameof(driveFileId));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType, nameof(mimeType));

        DriveConnectionId = driveConnectionId;
        DriveFileId = driveFileId.Trim();
        FileName = fileName.Trim();
        MimeType = mimeType.Trim();
        FileModifiedTime = fileModifiedTime;
        Status = DriveFileImportStatus.Pending;
        DiscoveredAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a new file import tracking record.
    /// </summary>
    public static DriveFileImport Create(
        Guid driveConnectionId,
        string driveFileId,
        string fileName,
        string mimeType,
        DateTimeOffset fileModifiedTime)
    {
        return new DriveFileImport(Guid.NewGuid(), driveConnectionId, driveFileId, fileName, mimeType, fileModifiedTime);
    }

    /// <summary>
    /// Marks the file as parsed with extracted content.
    /// Only valid from Pending status.
    /// </summary>
    public void MarkParsed(string parsedContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parsedContent, nameof(parsedContent));

        if (Status != DriveFileImportStatus.Pending)
            throw new InvalidOperationException($"Cannot mark as parsed from status '{Status}'. Must be Pending.");

        ParsedContent = parsedContent;
        Status = DriveFileImportStatus.Parsed;
        ParsedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the file as imported and links it to a meeting.
    /// Only valid from Parsed status.
    /// </summary>
    public void MarkImported(Guid meetingId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(meetingId, Guid.Empty, nameof(meetingId));

        if (Status != DriveFileImportStatus.Parsed)
            throw new InvalidOperationException($"Cannot mark as imported from status '{Status}'. Must be Parsed.");

        MatchedMeetingId = meetingId;
        Status = DriveFileImportStatus.Imported;
        ImportedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the file as skipped (not suitable for import).
    /// Valid from Pending or Parsed status.
    /// </summary>
    public void MarkSkipped(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        if (Status != DriveFileImportStatus.Pending && Status != DriveFileImportStatus.Parsed)
            throw new InvalidOperationException($"Cannot skip from status '{Status}'. Must be Pending or Parsed.");

        ErrorMessage = reason.Trim();
        Status = DriveFileImportStatus.Skipped;
    }

    /// <summary>
    /// Marks the file as having an error. Allowed from any status.
    /// </summary>
    public void MarkError(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage, nameof(errorMessage));

        ErrorMessage = errorMessage.Trim();
        Status = DriveFileImportStatus.Error;
    }

    /// <summary>
    /// Updates file metadata when the file is re-discovered with changes.
    /// </summary>
    public void UpdateFileMetadata(string fileName, string mimeType, DateTimeOffset fileModifiedTime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType, nameof(mimeType));

        FileName = fileName.Trim();
        MimeType = mimeType.Trim();
        FileModifiedTime = fileModifiedTime;
    }
}
