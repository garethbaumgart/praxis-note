using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Tests.Drive;

public class OverrideDuplicateTests
{
    private readonly IDriveConnectionRepository _connectionRepository = Substitute.For<IDriveConnectionRepository>();
    private readonly IDriveFileImportRepository _fileImportRepository = Substitute.For<IDriveFileImportRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OverrideDuplicate _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public OverrideDuplicateTests()
    {
        _sut = new OverrideDuplicate(_connectionRepository, _fileImportRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidFileImport_ClearsDuplicate()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var fileImport = DriveFileImport.Create(connection.Id, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        fileImport.MarkParsed("content", """{"title":"Test"}""");
        fileImport.MarkDuplicate(DeduplicationType.CalendarEvent, Guid.NewGuid(), "Some Meeting", 1.0m);

        _fileImportRepository.GetByIdAsync(fileImport.Id, Arg.Any<CancellationToken>())
            .Returns(fileImport);

        var command = new OverrideDuplicate.Command(_userId, _profileId, fileImport.Id);

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(DeduplicationType.None, fileImport.DuplicateType);
        Assert.Null(fileImport.MatchedMeetingId);
        Assert.Null(fileImport.DuplicateMatchTitle);
        Assert.Equal(0m, fileImport.DuplicateConfidence);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var fileImportId = Guid.NewGuid();
        _fileImportRepository.GetByIdAsync(fileImportId, Arg.Any<CancellationToken>())
            .Returns((DriveFileImport?)null);

        var command = new OverrideDuplicate.Command(_userId, _profileId, fileImportId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WithNoConnection_ThrowsInvalidOperationException()
    {
        // Arrange
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns((DriveConnection?)null);

        var command = new OverrideDuplicate.Command(_userId, _profileId, Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WithDifferentConnection_ThrowsInvalidOperationException()
    {
        // Arrange — file belongs to a different connection than the user's
        var userConnection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(userConnection);

        var otherConnectionId = Guid.NewGuid();
        var fileImport = DriveFileImport.Create(otherConnectionId, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        fileImport.MarkParsed("content", """{"title":"Test"}""");
        fileImport.MarkDuplicate(DeduplicationType.FuzzyMatch, Guid.NewGuid(), "Other Meeting", 0.8m);

        _fileImportRepository.GetByIdAsync(fileImport.Id, Arg.Any<CancellationToken>())
            .Returns(fileImport);

        var command = new OverrideDuplicate.Command(_userId, _profileId, fileImport.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(command));
    }
}
