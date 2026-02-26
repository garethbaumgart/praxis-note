using Microsoft.Extensions.Logging;
using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Application.Features.Drive.Services;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Tests.Drive;

public class DiscoverDriveFilesTests
{
    private readonly IDriveConnectionRepository _connectionRepository = Substitute.For<IDriveConnectionRepository>();
    private readonly IDriveFileImportRepository _fileImportRepository = Substitute.For<IDriveFileImportRepository>();
    private readonly IDriveService _driveService = Substitute.For<IDriveService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<DiscoverDriveFiles> _logger = Substitute.For<ILogger<DiscoverDriveFiles>>();
    private readonly DiscoverDriveFiles _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public DiscoverDriveFilesTests()
    {
        _sut = new DiscoverDriveFiles(_connectionRepository, _fileImportRepository, _driveService, _unitOfWork, _logger);
    }

    private DriveConnection CreateConnectionWithFolder(DateTimeOffset? tokenExpiresAt = null, DateTimeOffset? lastSyncedAt = null)
    {
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            tokenExpiresAt ?? DateTimeOffset.UtcNow.AddHours(1));
        connection.SetFolder("folder-123", "Meeting Notes");
        return connection;
    }

    [Fact]
    public async Task ExecuteAsync_WithNoConnection_ThrowsInvalidOperationException()
    {
        // Arrange
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns((DriveConnection?)null);

        var command = new DiscoverDriveFiles.Command(_userId, _profileId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WithNoFolderConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        // No folder set
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var command = new DiscoverDriveFiles.Command(_userId, _profileId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WithExpiredToken_RefreshesBeforeDiscovery()
    {
        // Arrange
        var connection = CreateConnectionWithFolder(tokenExpiresAt: DateTimeOffset.UtcNow.AddMinutes(-10));
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var refreshResult = new TokenRefreshResult("new-access", DateTimeOffset.UtcNow.AddHours(1), null);
        _driveService.RefreshAccessTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(refreshResult);

        _driveService.ListFilesAsync("new-access", "folder-123", Arg.Any<DateTimeOffset?>(), null, Arg.Any<CancellationToken>())
            .Returns(new DriveFileListResult([], null));

        var command = new DiscoverDriveFiles.Command(_userId, _profileId);

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        await _driveService.Received(1).RefreshAccessTokenAsync("refresh-token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNewFiles_CreatesTrackingRecords()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var files = new List<DriveFile>
        {
            new("file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow),
            new("file-2", "report.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", DateTimeOffset.UtcNow),
        };
        _driveService.ListFilesAsync("access-token", "folder-123", Arg.Any<DateTimeOffset?>(), null, Arg.Any<CancellationToken>())
            .Returns(new DriveFileListResult(files, null));

        _fileImportRepository.GetExistingDriveFileIdsAsync(connection.Id, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());

        var command = new DiscoverDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(2, result.NewFilesDiscovered);
        Assert.Equal(0, result.AlreadyTracked);
        Assert.Equal(2, result.TotalInFolder);
        await _fileImportRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<DriveFileImport>>(imports => imports.Count() == 2),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingFiles_SkipsDuplicates()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var files = new List<DriveFile>
        {
            new("file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow),
            new("file-2", "report.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", DateTimeOffset.UtcNow),
        };
        _driveService.ListFilesAsync("access-token", "folder-123", Arg.Any<DateTimeOffset?>(), null, Arg.Any<CancellationToken>())
            .Returns(new DriveFileListResult(files, null));

        // file-1 already tracked
        _fileImportRepository.GetExistingDriveFileIdsAsync(connection.Id, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "file-1" });

        var command = new DiscoverDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(1, result.NewFilesDiscovered);
        Assert.Equal(1, result.AlreadyTracked);
        Assert.Equal(2, result.TotalInFolder);
        await _fileImportRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<DriveFileImport>>(imports => imports.Count() == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNoFiles_RecordsSyncAndReturnsZero()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        _driveService.ListFilesAsync("access-token", "folder-123", Arg.Any<DateTimeOffset?>(), null, Arg.Any<CancellationToken>())
            .Returns(new DriveFileListResult([], null));

        var command = new DiscoverDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(0, result.NewFilesDiscovered);
        Assert.Equal(0, result.AlreadyTracked);
        Assert.Equal(0, result.TotalInFolder);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_UsesInitialCutoffDateOnFirstRun()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        connection.Configure("folder-123", "Meeting Notes", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), 15, false);
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        _driveService.ListFilesAsync("access-token", "folder-123", Arg.Any<DateTimeOffset?>(), null, Arg.Any<CancellationToken>())
            .Returns(new DriveFileListResult([], null));

        var command = new DiscoverDriveFiles.Command(_userId, _profileId);

        // Act
        await _sut.ExecuteAsync(command);

        // Assert — verify ListFilesAsync was called with a modifiedAfter value matching the cutoff date
        await _driveService.Received(1).ListFilesAsync(
            "access-token",
            "folder-123",
            Arg.Is<DateTimeOffset?>(d => d.HasValue && d.Value.Date == DateTime.UtcNow.AddDays(-7).Date),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_UsesLastSyncedAtOnSubsequentRuns()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        connection.Configure("folder-123", "Meeting Notes", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)), 15, false);
        connection.RecordSync(); // Simulate a previous sync
        var lastSyncedAt = connection.LastSyncedAt;

        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        _driveService.ListFilesAsync("access-token", "folder-123", Arg.Any<DateTimeOffset?>(), null, Arg.Any<CancellationToken>())
            .Returns(new DriveFileListResult([], null));

        var command = new DiscoverDriveFiles.Command(_userId, _profileId);

        // Act
        await _sut.ExecuteAsync(command);

        // Assert — verify ListFilesAsync was called with lastSyncedAt, not the cutoff date
        await _driveService.Received(1).ListFilesAsync(
            "access-token",
            "folder-123",
            Arg.Is<DateTimeOffset?>(d => d.HasValue && d.Value == lastSyncedAt),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PaginatesThroughAllPages()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var page1Files = new List<DriveFile>
        {
            new("file-1", "notes1.txt", "text/plain", DateTimeOffset.UtcNow),
        };
        var page2Files = new List<DriveFile>
        {
            new("file-2", "notes2.txt", "text/plain", DateTimeOffset.UtcNow),
        };

        _driveService.ListFilesAsync("access-token", "folder-123", Arg.Any<DateTimeOffset?>(), null, Arg.Any<CancellationToken>())
            .Returns(new DriveFileListResult(page1Files, "next-page-token"));
        _driveService.ListFilesAsync("access-token", "folder-123", Arg.Any<DateTimeOffset?>(), "next-page-token", Arg.Any<CancellationToken>())
            .Returns(new DriveFileListResult(page2Files, null));

        _fileImportRepository.GetExistingDriveFileIdsAsync(connection.Id, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());

        var command = new DiscoverDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(2, result.NewFilesDiscovered);
        Assert.Equal(2, result.TotalInFolder);
        await _driveService.Received(2).ListFilesAsync(
            "access-token", "folder-123", Arg.Any<DateTimeOffset?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
