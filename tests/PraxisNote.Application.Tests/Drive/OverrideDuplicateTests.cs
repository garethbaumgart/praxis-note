using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Tests.Drive;

public class OverrideDuplicateTests
{
    private readonly IDriveFileImportRepository _fileImportRepository = Substitute.For<IDriveFileImportRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OverrideDuplicate _sut;

    public OverrideDuplicateTests()
    {
        _sut = new OverrideDuplicate(_fileImportRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidFileImport_ClearsDuplicate()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var fileImport = DriveFileImport.Create(connectionId, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        fileImport.MarkParsed("content", """{"title":"Test"}""");
        fileImport.MarkDuplicate(DeduplicationType.CalendarEvent, Guid.NewGuid(), "Some Meeting", 1.0m);

        _fileImportRepository.GetByIdAsync(fileImport.Id, Arg.Any<CancellationToken>())
            .Returns(fileImport);

        var command = new OverrideDuplicate.Command(fileImport.Id);

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
        var fileImportId = Guid.NewGuid();
        _fileImportRepository.GetByIdAsync(fileImportId, Arg.Any<CancellationToken>())
            .Returns((DriveFileImport?)null);

        var command = new OverrideDuplicate.Command(fileImportId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(command));
    }
}
