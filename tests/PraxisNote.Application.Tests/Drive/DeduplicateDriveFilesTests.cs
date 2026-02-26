using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Application.Features.Drive.Services;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Tests.Drive;

public class DeduplicateDriveFilesTests
{
    private readonly IDriveConnectionRepository _connectionRepository = Substitute.For<IDriveConnectionRepository>();
    private readonly IDriveFileImportRepository _fileImportRepository = Substitute.For<IDriveFileImportRepository>();
    private readonly IDriveDeduplicationService _deduplicationService = Substitute.For<IDriveDeduplicationService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeduplicateDriveFiles _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public DeduplicateDriveFilesTests()
    {
        _sut = new DeduplicateDriveFiles(_connectionRepository, _fileImportRepository, _deduplicationService, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoConnection_ThrowsInvalidOperationException()
    {
        // Arrange
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns((DriveConnection?)null);

        var command = new DeduplicateDriveFiles.Command(_userId, _profileId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WithParsedFiles_CallsDeduplicationService()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var parsedFiles = new List<DriveFileImport>
        {
            DriveFileImport.Create(connection.Id, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow),
        };
        // Mark as parsed so they are in the correct status
        parsedFiles[0].MarkParsed("content", """{"title":"Test"}""");

        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Parsed, Arg.Any<CancellationToken>())
            .Returns(parsedFiles);

        var command = new DeduplicateDriveFiles.Command(_userId, _profileId);

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        await _deduplicationService.Received(1).DeduplicateAsync(
            _userId, _profileId, parsedFiles, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNoParsedFiles_ReturnsZeros()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Parsed, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport>());

        var command = new DeduplicateDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(0, result.Checked);
        Assert.Equal(0, result.DefiniteDuplicates);
        Assert.Equal(0, result.PossibleDuplicates);
        await _deduplicationService.DidNotReceive().DeduplicateAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IReadOnlyList<DriveFileImport>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCorrectDefiniteAndPossibleCounts()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var file1 = DriveFileImport.Create(connection.Id, "file-1", "notes1.txt", "text/plain", DateTimeOffset.UtcNow);
        file1.MarkParsed("content1", """{"title":"Test1"}""");
        var file2 = DriveFileImport.Create(connection.Id, "file-2", "notes2.txt", "text/plain", DateTimeOffset.UtcNow);
        file2.MarkParsed("content2", """{"title":"Test2"}""");
        var file3 = DriveFileImport.Create(connection.Id, "file-3", "notes3.txt", "text/plain", DateTimeOffset.UtcNow);
        file3.MarkParsed("content3", """{"title":"Test3"}""");

        var parsedFiles = new List<DriveFileImport> { file1, file2, file3 };
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Parsed, Arg.Any<CancellationToken>())
            .Returns(parsedFiles);

        // Simulate deduplication marking files
        _deduplicationService.DeduplicateAsync(_userId, _profileId, parsedFiles, Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var files = ci.ArgAt<IReadOnlyList<DriveFileImport>>(2);
                files[0].MarkDuplicate(DeduplicationType.CalendarEvent, Guid.NewGuid(), "Meeting 1", 1.0m);
                files[1].MarkDuplicate(DeduplicationType.FuzzyMatch, Guid.NewGuid(), "Meeting 2", 0.75m);
                // file3 remains unmatched
                return Task.CompletedTask;
            });

        var command = new DeduplicateDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(3, result.Checked);
        Assert.Equal(1, result.DefiniteDuplicates);
        Assert.Equal(1, result.PossibleDuplicates);
    }
}
