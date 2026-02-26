using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Application.Features.Drive.Services;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Application.Tests.Drive;

public class DriveSyncOrchestratorTests
{
    private readonly IDriveConnectionRepository _connectionRepository = Substitute.For<IDriveConnectionRepository>();
    private readonly IDriveFileImportRepository _fileImportRepository = Substitute.For<IDriveFileImportRepository>();
    private readonly IDriveService _driveService = Substitute.For<IDriveService>();
    private readonly ParseTranscriptForImport _parseTranscript;
    private readonly ITranscriptExtractor _transcriptExtractor = Substitute.For<ITranscriptExtractor>();
    private readonly IDriveDeduplicationService _deduplicationService = Substitute.For<IDriveDeduplicationService>();
    private readonly ConfirmDriveImport _confirmDriveImport;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IMeetingRepository _meetingRepository = Substitute.For<IMeetingRepository>();
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<DriveSyncOrchestrator> _logger = Substitute.For<ILogger<DriveSyncOrchestrator>>();
    private readonly DriveSyncOrchestrator _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public DriveSyncOrchestratorTests()
    {
        var meetingAnalyzer = Substitute.For<IMeetingAnalyzer>();
        meetingAnalyzer.ParseTranscriptForImportAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new TranscriptImportResult(
                "Test Meeting", DateTimeOffset.UtcNow, "Alice, Bob",
                "Summary", ["Key point 1"], ["Decision 1"],
                [new ExtractedActionItem("Action 1", "Alice")],
                ["tag1"], true, null));

        _parseTranscript = new ParseTranscriptForImport(meetingAnalyzer, _transcriptExtractor);

        var confirmTranscriptImport = new ConfirmTranscriptImport(
            _meetingRepository, _tagRepository, _unitOfWork, _fileImportRepository);
        _confirmDriveImport = new ConfirmDriveImport(confirmTranscriptImport, _fileImportRepository, _connectionRepository);

        var syncSettings = Options.Create(new DriveSyncSettings());
        _sut = new DriveSyncOrchestrator(
            _connectionRepository, _fileImportRepository, _driveService,
            _parseTranscript, _transcriptExtractor, _deduplicationService,
            _confirmDriveImport, _userRepository, _unitOfWork, syncSettings, _logger);
    }

    private DriveConnection CreateConfiguredConnection(bool autoAcceptTags = false)
    {
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        connection.Configure("folder-123", "Meeting Notes", null, 15, autoAcceptTags);
        return connection;
    }

    private void SetupDriveFiles(DriveConnection connection, params DriveFile[] files)
    {
        _driveService.ListFilesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new DriveFileListResult(files.ToList(), null));

        _fileImportRepository.GetExistingDriveFileIdsAsync(
            connection.Id, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());
    }

    #region SyncConnectionAsync Tests

    [Fact]
    public async Task SyncConnectionAsync_WithNoConnection_ReturnsError()
    {
        // Arrange
        _connectionRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DriveConnection?)null);

        // Act
        var result = await _sut.SyncConnectionAsync(Guid.NewGuid());

        // Assert
        Assert.Equal("Connection not found", result.Error);
        Assert.Equal(0, result.FilesDiscovered);
    }

    [Fact]
    public async Task SyncConnectionAsync_NoNewFiles_ReturnsZeroCounts()
    {
        // Arrange
        var connection = CreateConfiguredConnection();
        _connectionRepository.GetByIdAsync(connection.Id, Arg.Any<CancellationToken>())
            .Returns(connection);

        _driveService.ListFilesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new DriveFileListResult([], null));

        // Act
        var result = await _sut.SyncConnectionAsync(connection.Id);

        // Assert
        Assert.Equal(0, result.FilesDiscovered);
        Assert.Equal(0, result.FilesImported);
        Assert.Equal(0, result.FilesPendingReview);
        Assert.Equal(0, result.FilesErrored);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SyncConnectionAsync_QueuesFilesForReview_WhenAutoAcceptOff()
    {
        // Arrange
        var connection = CreateConfiguredConnection(autoAcceptTags: false);
        _connectionRepository.GetByIdAsync(connection.Id, Arg.Any<CancellationToken>())
            .Returns(connection);

        var files = new[] { new DriveFile("file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow) };
        SetupDriveFiles(connection, files);

        _driveService.DownloadFileAsync(Arg.Any<string>(), "file-1", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("meeting transcript")));
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("meeting transcript");

        // Act
        var result = await _sut.SyncConnectionAsync(connection.Id);

        // Assert
        Assert.Equal(1, result.FilesDiscovered);
        Assert.Equal(0, result.FilesImported);
        Assert.Equal(1, result.FilesPendingReview);
        Assert.Equal(0, result.FilesErrored);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SyncConnectionAsync_TokenExpired_RecordsFailureAndReturnsError()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "expired-token", "refresh-token",
            DateTimeOffset.UtcNow.AddMinutes(-10)); // Expired
        connection.Configure("folder-123", "Meeting Notes", null, 15, false);

        _connectionRepository.GetByIdAsync(connection.Id, Arg.Any<CancellationToken>())
            .Returns(connection);

        _driveService.RefreshAccessTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Token revoked"));

        // Act
        var result = await _sut.SyncConnectionAsync(connection.Id);

        // Assert
        Assert.Equal("OAuth token expired. Please reconnect Google Drive.", result.Error);
        Assert.Equal(0, result.FilesDiscovered);
        Assert.NotNull(connection.LastSyncError);
        Assert.Equal(1, connection.ConsecutiveFailures);
    }

    [Fact]
    public async Task SyncConnectionAsync_PerFileError_ContinuesAndReportsErrors()
    {
        // Arrange
        var connection = CreateConfiguredConnection(autoAcceptTags: false);
        _connectionRepository.GetByIdAsync(connection.Id, Arg.Any<CancellationToken>())
            .Returns(connection);

        var files = new[]
        {
            new DriveFile("file-1", "good.txt", "text/plain", DateTimeOffset.UtcNow),
            new DriveFile("file-2", "bad.xyz", "application/octet-stream", DateTimeOffset.UtcNow)
        };
        SetupDriveFiles(connection, files);

        // First file downloads fine
        _driveService.DownloadFileAsync(Arg.Any<string>(), "file-1", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content")));
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("content");

        // Second file has unsupported mime type - will throw in ExtractTextFromDriveFileAsync

        // Act
        var result = await _sut.SyncConnectionAsync(connection.Id);

        // Assert
        Assert.Equal(2, result.FilesDiscovered);
        Assert.Equal(1, result.FilesErrored);
        // The good file should be pending review
        Assert.Equal(1, result.FilesPendingReview);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SyncConnectionAsync_FolderNotFound_RecordsFailure()
    {
        // Arrange
        var connection = CreateConfiguredConnection();
        _connectionRepository.GetByIdAsync(connection.Id, Arg.Any<CancellationToken>())
            .Returns(connection);

        _driveService.ListFilesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Folder not found"));

        // Act
        var result = await _sut.SyncConnectionAsync(connection.Id);

        // Assert
        Assert.Equal("Configured folder no longer exists or is inaccessible.", result.Error);
        Assert.NotNull(connection.LastSyncError);
    }

    [Fact]
    public async Task SyncConnectionAsync_AllFilesAlreadyTracked_ReturnsZero()
    {
        // Arrange
        var connection = CreateConfiguredConnection();
        _connectionRepository.GetByIdAsync(connection.Id, Arg.Any<CancellationToken>())
            .Returns(connection);

        var files = new[] { new DriveFile("file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow) };
        _driveService.ListFilesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new DriveFileListResult(files.ToList(), null));

        // All files already tracked
        _fileImportRepository.GetExistingDriveFileIdsAsync(
            connection.Id, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "file-1" });

        // Act
        var result = await _sut.SyncConnectionAsync(connection.Id);

        // Assert
        Assert.Equal(0, result.FilesDiscovered);
        Assert.Equal(0, result.FilesImported);
        Assert.Null(result.Error);
    }

    #endregion

    #region ManualSyncAsync Tests

    [Fact]
    public async Task ManualSyncAsync_ClearsErrorStateFirst()
    {
        // Arrange
        var connection = CreateConfiguredConnection();
        connection.RecordSyncFailure("Previous error");
        Assert.Equal(1, connection.ConsecutiveFailures);

        _connectionRepository.GetByIdAsync(connection.Id, Arg.Any<CancellationToken>())
            .Returns(connection);

        _driveService.ListFilesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new DriveFileListResult([], null));

        // Act
        var result = await _sut.ManualSyncAsync(_userId, _profileId, connection.Id);

        // Assert - error was cleared before sync ran
        Assert.Null(result.Error);
        Assert.Equal(0, connection.ConsecutiveFailures);
    }

    [Fact]
    public async Task ManualSyncAsync_WithWrongUser_ReturnsError()
    {
        // Arrange
        var connection = CreateConfiguredConnection();
        _connectionRepository.GetByIdAsync(connection.Id, Arg.Any<CancellationToken>())
            .Returns(connection);

        var wrongUserId = Guid.NewGuid();

        // Act
        var result = await _sut.ManualSyncAsync(wrongUserId, _profileId, connection.Id);

        // Assert
        Assert.Equal("Connection not found", result.Error);
    }

    #endregion
}
