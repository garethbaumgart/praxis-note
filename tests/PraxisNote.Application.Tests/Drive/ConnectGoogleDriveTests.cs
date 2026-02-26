using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Application.Tests.Drive;

public class ConnectGoogleDriveTests
{
    private readonly IDriveConnectionRepository _repository = Substitute.For<IDriveConnectionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ConnectGoogleDrive _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public ConnectGoogleDriveTests()
    {
        _sut = new ConnectGoogleDrive(_repository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_NoExistingConnection_CreatesNewConnection()
    {
        // Arrange
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns((DriveConnection?)null);

        var command = new ConnectGoogleDrive.Command(
            _userId, _profileId, "access-token", "refresh-token", DateTimeOffset.UtcNow.AddHours(1));

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<DriveConnection>(c =>
                c.UserId == _userId &&
                c.ProfileId == _profileId &&
                c.Provider == "Google" &&
                c.AccessToken == "access-token" &&
                c.RefreshToken == "refresh-token"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ExistingConnection_RemovesOldAndCreatesNew()
    {
        // Arrange
        var existing = DriveConnection.Create(
            _userId, _profileId, "Google", "old-access", "old-refresh", DateTimeOffset.UtcNow.AddHours(1));
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new ConnectGoogleDrive.Command(
            _userId, _profileId, "new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(2));

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        _repository.Received(1).Remove(existing);
        await _repository.Received(1).AddAsync(
            Arg.Is<DriveConnection>(c => c.AccessToken == "new-access"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
