using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Application.Tests.Drive;

public class UpdateDriveSettingsTests
{
    private readonly IDriveConnectionRepository _repository = Substitute.For<IDriveConnectionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateDriveSettings _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public UpdateDriveSettingsTests()
    {
        _sut = new UpdateDriveSettings(_repository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCommand_UpdatesConnection()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var command = new UpdateDriveSettings.Command(
            _userId, _profileId, "folder-123", "Meeting Notes",
            new DateOnly(2026, 1, 15), 30, true, "America/New_York");

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal("folder-123", connection.FolderId);
        Assert.Equal("Meeting Notes", connection.FolderName);
        Assert.Equal(new DateOnly(2026, 1, 15), connection.InitialImportCutoffDate);
        Assert.Equal(30, connection.SyncFrequencyMinutes);
        Assert.True(connection.AutoAcceptTags);
        Assert.Equal("America/New_York", connection.TimeZone);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNoConnection_ThrowsInvalidOperationException()
    {
        // Arrange
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns((DriveConnection?)null);

        var command = new UpdateDriveSettings.Command(
            _userId, _profileId, "folder-123", "Meeting Notes", null, 15, false, null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidFrequency_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var command = new UpdateDriveSettings.Command(
            _userId, _profileId, "folder-123", "Meeting Notes", null, 45, false, null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.ExecuteAsync(command));
    }
}
