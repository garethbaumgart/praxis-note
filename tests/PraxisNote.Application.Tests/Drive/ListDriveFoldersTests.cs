using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Application.Features.Drive.Services;
using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Application.Tests.Drive;

public class ListDriveFoldersTests
{
    private readonly IDriveConnectionRepository _repository = Substitute.For<IDriveConnectionRepository>();
    private readonly IDriveService _driveService = Substitute.For<IDriveService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ListDriveFolders _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public ListDriveFoldersTests()
    {
        _sut = new ListDriveFolders(_repository, _driveService, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidConnection_ReturnsFolders()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var folders = new List<DriveFolder>
        {
            new("folder-1", "Meeting Notes", DateTimeOffset.UtcNow),
            new("folder-2", "Documents", DateTimeOffset.UtcNow.AddDays(-1)),
        };
        _driveService.ListFoldersAsync("access-token", null, Arg.Any<CancellationToken>())
            .Returns(folders);

        var query = new ListDriveFolders.Query(_userId, _profileId, null);

        // Act
        var result = await _sut.ExecuteAsync(query);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("folder-1", result[0].Id);
        Assert.Equal("Meeting Notes", result[0].Name);
        Assert.Equal("folder-2", result[1].Id);
        Assert.Equal("Documents", result[1].Name);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoConnection_ThrowsInvalidOperationException()
    {
        // Arrange
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns((DriveConnection?)null);

        var query = new ListDriveFolders.Query(_userId, _profileId, null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(query));
    }

    [Fact]
    public async Task ExecuteAsync_WithExpiredToken_RefreshesTokenBeforeListing()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "old-access", "refresh-token",
            DateTimeOffset.UtcNow.AddMinutes(-10)); // expired
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var refreshResult = new TokenRefreshResult("new-access", DateTimeOffset.UtcNow.AddHours(1), null);
        _driveService.RefreshAccessTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(refreshResult);

        _driveService.ListFoldersAsync("new-access", null, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFolder>());

        var query = new ListDriveFolders.Query(_userId, _profileId, null);

        // Act
        await _sut.ExecuteAsync(query);

        // Assert
        await _driveService.Received(1).RefreshAccessTokenAsync("refresh-token", Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _driveService.Received(1).ListFoldersAsync("new-access", null, Arg.Any<CancellationToken>());
    }
}
