using NSubstitute;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Tests.Drive;

public class GetDriveFileImportsTests
{
    private readonly IDriveConnectionRepository _connectionRepository = Substitute.For<IDriveConnectionRepository>();
    private readonly IDriveFileImportRepository _fileImportRepository = Substitute.For<IDriveFileImportRepository>();
    private readonly GetDriveFileImports _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public GetDriveFileImportsTests()
    {
        _sut = new GetDriveFileImports(_connectionRepository, _fileImportRepository);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoFilter_ReturnsAllFiles()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var imports = new List<DriveFileImport>
        {
            DriveFileImport.Create(connection.Id, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow),
            DriveFileImport.Create(connection.Id, "file-2", "report.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", DateTimeOffset.UtcNow),
        };
        _fileImportRepository.GetByConnectionIdAsync(connection.Id, Arg.Any<CancellationToken>())
            .Returns(imports);

        var query = new GetDriveFileImports.Query(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(query);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("file-1", result[0].DriveFileId);
        Assert.Equal("notes.txt", result[0].FileName);
        Assert.Equal("Pending", result[0].Status);
        Assert.Equal("file-2", result[1].DriveFileId);
    }

    [Fact]
    public async Task ExecuteAsync_WithStatusFilter_ReturnsFilteredFiles()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var pendingImports = new List<DriveFileImport>
        {
            DriveFileImport.Create(connection.Id, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow),
        };
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(pendingImports);

        var query = new GetDriveFileImports.Query(_userId, _profileId, DriveFileImportStatus.Pending);

        // Act
        var result = await _sut.ExecuteAsync(query);

        // Assert
        Assert.Single(result);
        Assert.Equal("file-1", result[0].DriveFileId);
        await _fileImportRepository.Received(1).GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>());
        await _fileImportRepository.DidNotReceive().GetByConnectionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNoConnection_ThrowsInvalidOperationException()
    {
        // Arrange
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns((DriveConnection?)null);

        var query = new GetDriveFileImports.Query(_userId, _profileId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(query));
    }
}
