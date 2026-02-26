using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Domain.Tests.Aggregates;

public class DriveFileImportTests
{
    private readonly Guid _validConnectionId = Guid.NewGuid();
    private const string ValidDriveFileId = "drive-file-123";
    private const string ValidFileName = "meeting-notes.txt";
    private const string ValidMimeType = "text/plain";
    private readonly DateTimeOffset _validModifiedTime = DateTimeOffset.UtcNow.AddHours(-1);

    #region Create Tests

    [Fact]
    public void Create_WithValidInputs_CreatesWithPendingStatus()
    {
        // Act
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Assert
        Assert.NotEqual(Guid.Empty, import.Id);
        Assert.Equal(_validConnectionId, import.DriveConnectionId);
        Assert.Equal(ValidDriveFileId, import.DriveFileId);
        Assert.Equal(ValidFileName, import.FileName);
        Assert.Equal(ValidMimeType, import.MimeType);
        Assert.Equal(_validModifiedTime, import.FileModifiedTime);
        Assert.Equal(DriveFileImportStatus.Pending, import.Status);
        Assert.Null(import.MatchedMeetingId);
        Assert.Null(import.ParsedContent);
        Assert.Null(import.ParsedAt);
        Assert.Null(import.ImportedAt);
        Assert.Null(import.ErrorMessage);
    }

    [Fact]
    public void Create_WithEmptyConnectionId_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DriveFileImport.Create(Guid.Empty, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyDriveFileId_ThrowsArgumentException(string? invalidId)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            DriveFileImport.Create(_validConnectionId, invalidId!, ValidFileName, ValidMimeType, _validModifiedTime));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyFileName_ThrowsArgumentException(string? invalidName)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            DriveFileImport.Create(_validConnectionId, ValidDriveFileId, invalidName!, ValidMimeType, _validModifiedTime));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyMimeType_ThrowsArgumentException(string? invalidMime)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            DriveFileImport.Create(_validConnectionId, ValidDriveFileId, ValidFileName, invalidMime!, _validModifiedTime));
    }

    [Fact]
    public void Create_SetsDiscoveredAtToUtcNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Assert
        var after = DateTimeOffset.UtcNow;
        Assert.InRange(import.DiscoveredAt, before, after);
    }

    #endregion

    #region MarkParsed Tests

    [Fact]
    public void MarkParsed_FromPending_SetsStatusAndParsedAt()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        var before = DateTimeOffset.UtcNow;

        // Act
        import.MarkParsed("Parsed document content", """{"title":"Test"}""");

        // Assert
        Assert.Equal(DriveFileImportStatus.Parsed, import.Status);
        Assert.Equal("Parsed document content", import.ParsedContent);
        Assert.Equal("""{"title":"Test"}""", import.ParsedResultJson);
        Assert.NotNull(import.ParsedAt);
        Assert.InRange(import.ParsedAt.Value, before, DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkParsed_WithEmptyContent_ThrowsArgumentException(string? invalidContent)
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => import.MarkParsed(invalidContent!, """{"title":"Test"}"""));
    }

    [Fact]
    public void MarkParsed_FromImported_ThrowsInvalidOperationException()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkParsed("content", "{}");
        import.MarkImported(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => import.MarkParsed("new content", "{}"));
    }

    [Fact]
    public void MarkParsed_FromParsed_ThrowsInvalidOperationException()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkParsed("content", "{}");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => import.MarkParsed("new content", "{}"));
    }

    [Fact]
    public void MarkParsed_FromSkipped_ThrowsInvalidOperationException()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkSkipped("reason");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => import.MarkParsed("content", "{}"));
    }

    [Fact]
    public void MarkParsed_FromError_ThrowsInvalidOperationException()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkError("error");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => import.MarkParsed("content", "{}"));
    }

    #endregion

    #region MarkImported Tests

    [Fact]
    public void MarkImported_FromParsed_SetsStatusAndMatchedMeetingId()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkParsed("content", "{}");
        var meetingId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        // Act
        import.MarkImported(meetingId);

        // Assert
        Assert.Equal(DriveFileImportStatus.Imported, import.Status);
        Assert.Equal(meetingId, import.MatchedMeetingId);
        Assert.NotNull(import.ImportedAt);
        Assert.InRange(import.ImportedAt.Value, before, DateTimeOffset.UtcNow);
    }

    [Fact]
    public void MarkImported_WithEmptyMeetingId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkParsed("content", "{}");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => import.MarkImported(Guid.Empty));
    }

    [Fact]
    public void MarkImported_FromPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => import.MarkImported(Guid.NewGuid()));
    }

    #endregion

    #region MarkSkipped Tests

    [Fact]
    public void MarkSkipped_FromPending_SetsStatus()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Act
        import.MarkSkipped("Not a meeting transcript");

        // Assert
        Assert.Equal(DriveFileImportStatus.Skipped, import.Status);
        Assert.Equal("Not a meeting transcript", import.ErrorMessage);
    }

    [Fact]
    public void MarkSkipped_FromParsed_SetsStatus()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkParsed("content", "{}");

        // Act
        import.MarkSkipped("User chose to skip");

        // Assert
        Assert.Equal(DriveFileImportStatus.Skipped, import.Status);
    }

    [Fact]
    public void MarkSkipped_DoesNotSetMeetingId()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkParsed("content", "{}");

        // Act
        import.MarkSkipped("Skipped by user during import review");

        // Assert
        Assert.Null(import.ImportedAt);
        Assert.Null(import.MatchedMeetingId);
    }

    [Fact]
    public void MarkSkipped_FromImported_ThrowsInvalidOperationException()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkParsed("content", "{}");
        import.MarkImported(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => import.MarkSkipped("reason"));
    }

    [Fact]
    public void MarkSkipped_FromError_ThrowsInvalidOperationException()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkError("error");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => import.MarkSkipped("reason"));
    }

    [Fact]
    public void MarkSkipped_FromSkipped_ThrowsInvalidOperationException()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkSkipped("first reason");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => import.MarkSkipped("second reason"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkSkipped_WithEmptyReason_ThrowsArgumentException(string? invalidReason)
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => import.MarkSkipped(invalidReason!));
    }

    #endregion

    #region MarkError Tests

    [Fact]
    public void MarkError_FromAnyStatus_SetsErrorMessage()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Act
        import.MarkError("Download failed");

        // Assert
        Assert.Equal(DriveFileImportStatus.Error, import.Status);
        Assert.Equal("Download failed", import.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkError_WithEmptyMessage_ThrowsArgumentException(string? invalidMessage)
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => import.MarkError(invalidMessage!));
    }

    [Fact]
    public void MarkError_PreservesOtherFields()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkParsed("parsed content", """{"title":"Test"}""");

        // Act
        import.MarkError("Processing failed");

        // Assert
        Assert.Equal(DriveFileImportStatus.Error, import.Status);
        Assert.Equal("Processing failed", import.ErrorMessage);
        Assert.Equal("parsed content", import.ParsedContent);
        Assert.NotNull(import.ParsedAt);
        Assert.Equal(ValidFileName, import.FileName);
        Assert.Equal(ValidMimeType, import.MimeType);
    }

    #endregion

    #region UpdateFileMetadata Tests

    [Fact]
    public void UpdateFileMetadata_UpdatesFileNameAndMimeType()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        var newModifiedTime = DateTimeOffset.UtcNow;

        // Act
        import.UpdateFileMetadata("updated-name.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", newModifiedTime);

        // Assert
        Assert.Equal("updated-name.docx", import.FileName);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", import.MimeType);
        Assert.Equal(newModifiedTime, import.FileModifiedTime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateFileMetadata_WithEmptyFileName_ThrowsArgumentException(string? invalidName)
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            import.UpdateFileMetadata(invalidName!, ValidMimeType, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateFileMetadata_WithEmptyMimeType_ThrowsArgumentException(string? invalidMime)
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            import.UpdateFileMetadata(ValidFileName, invalidMime!, DateTimeOffset.UtcNow));
    }

    #endregion

    #region MarkParsed with ParsedResultJson Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkParsed_WithEmptyResultJson_ThrowsArgumentException(string? invalidJson)
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => import.MarkParsed("valid content", invalidJson!));
    }

    [Fact]
    public void MarkParsed_WithValidInputs_StoresBothContentAndJson()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        const string content = "Meeting transcript text";
        const string resultJson = """{"title":"Weekly Standup","summary":"Team discussed progress"}""";

        // Act
        import.MarkParsed(content, resultJson);

        // Assert
        Assert.Equal(DriveFileImportStatus.Parsed, import.Status);
        Assert.Equal(content, import.ParsedContent);
        Assert.Equal(resultJson, import.ParsedResultJson);
        Assert.NotNull(import.ParsedAt);
    }

    #endregion

    #region MarkDuplicate Tests

    [Fact]
    public void MarkDuplicate_WithCalendarEvent_SetsAllProperties()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        var meetingId = Guid.NewGuid();

        // Act
        import.MarkDuplicate(DeduplicationType.CalendarEvent, meetingId, "Weekly Standup", 1.0m);

        // Assert
        Assert.Equal(DeduplicationType.CalendarEvent, import.DuplicateType);
        Assert.Equal(meetingId, import.MatchedMeetingId);
        Assert.Equal("Weekly Standup", import.DuplicateMatchTitle);
        Assert.Equal(1.0m, import.DuplicateConfidence);
    }

    [Fact]
    public void MarkDuplicate_WithFuzzyMatch_SetsConfidence()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        var meetingId = Guid.NewGuid();

        // Act
        import.MarkDuplicate(DeduplicationType.FuzzyMatch, meetingId, "Sprint Planning", 0.75m);

        // Assert
        Assert.Equal(DeduplicationType.FuzzyMatch, import.DuplicateType);
        Assert.Equal(0.75m, import.DuplicateConfidence);
        Assert.Equal("Sprint Planning", import.DuplicateMatchTitle);
    }

    [Fact]
    public void MarkDuplicate_WithTypeNone_ThrowsArgumentException()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            import.MarkDuplicate(DeduplicationType.None, Guid.NewGuid(), "Title", 1.0m));
    }

    [Fact]
    public void MarkDuplicate_WithEmptyMeetingId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            import.MarkDuplicate(DeduplicationType.CalendarEvent, Guid.Empty, "Title", 1.0m));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void MarkDuplicate_WithInvalidConfidence_ThrowsArgumentOutOfRangeException(double confidence)
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            import.MarkDuplicate(DeduplicationType.FuzzyMatch, Guid.NewGuid(), "Title", (decimal)confidence));
    }

    [Fact]
    public void MarkDuplicate_WithWhitespaceTitle_StoresNull()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        var meetingId = Guid.NewGuid();

        // Act
        import.MarkDuplicate(DeduplicationType.CalendarEvent, meetingId, "   ", 1.0m);

        // Assert
        Assert.Null(import.DuplicateMatchTitle);
    }

    #endregion

    #region ClearDuplicate Tests

    [Fact]
    public void ClearDuplicate_ResetsAllDuplicateFields()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        var meetingId = Guid.NewGuid();
        import.MarkDuplicate(DeduplicationType.CalendarEvent, meetingId, "Weekly Standup", 1.0m);

        // Act
        import.ClearDuplicate();

        // Assert
        Assert.Equal(DeduplicationType.None, import.DuplicateType);
        Assert.Null(import.MatchedMeetingId);
        Assert.Null(import.DuplicateMatchTitle);
        Assert.Equal(0m, import.DuplicateConfidence);
    }

    [Fact]
    public void ClearDuplicate_PreservesOtherFields()
    {
        // Arrange
        var import = DriveFileImport.Create(
            _validConnectionId, ValidDriveFileId, ValidFileName, ValidMimeType, _validModifiedTime);
        import.MarkParsed("parsed content", """{"title":"Test"}""");
        import.MarkDuplicate(DeduplicationType.FuzzyMatch, Guid.NewGuid(), "Some Meeting", 0.8m);

        // Act
        import.ClearDuplicate();

        // Assert — dedup fields are cleared
        Assert.Equal(DeduplicationType.None, import.DuplicateType);
        Assert.Null(import.MatchedMeetingId);
        Assert.Null(import.DuplicateMatchTitle);
        Assert.Equal(0m, import.DuplicateConfidence);
        // Assert — other fields preserved
        Assert.Equal(DriveFileImportStatus.Parsed, import.Status);
        Assert.Equal("parsed content", import.ParsedContent);
        Assert.Equal(ValidFileName, import.FileName);
        Assert.Equal(ValidMimeType, import.MimeType);
        Assert.NotNull(import.ParsedAt);
    }

    #endregion
}
