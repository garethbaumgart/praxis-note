using NSubstitute;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Tests.Drive;

public class GetDriveConnectionStatusTests
{
    private readonly IDriveConnectionRepository _repository = Substitute.For<IDriveConnectionRepository>();
    private readonly IDriveFileImportRepository _fileImportRepository = Substitute.For<IDriveFileImportRepository>();
    private readonly GetDriveConnectionStatus _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public GetDriveConnectionStatusTests()
    {
        _sut = new GetDriveConnectionStatus(_repository, _fileImportRepository);
    }

    [Fact]
    public async Task ExecuteAsync_NoConnection_ReturnsNotConnected()
    {
        // Arrange
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns((DriveConnection?)null);

        var query = new GetDriveConnectionStatus.Query(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(query);

        // Assert
        Assert.False(result.IsConnected);
        Assert.Null(result.Provider);
        Assert.Null(result.ConnectedAt);
        Assert.Null(result.LastSyncedAt);
        Assert.Null(result.FolderName);
        Assert.Null(result.FolderId);
        Assert.False(result.IsConfigured);
        Assert.False(result.AutoAcceptTags);
    }

    [Fact]
    public async Task ExecuteAsync_WithConnection_ReturnsConnectedStatus()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token", DateTimeOffset.UtcNow.AddHours(1));
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var query = new GetDriveConnectionStatus.Query(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(query);

        // Assert
        Assert.True(result.IsConnected);
        Assert.Equal("Google", result.Provider);
        Assert.NotNull(result.ConnectedAt);
        Assert.Null(result.LastSyncedAt);
        Assert.Null(result.FolderName);
        Assert.False(result.IsConfigured);
    }

    [Fact]
    public async Task ExecuteAsync_WithConnectionAndFolder_ReturnsFolderNameAndIsConfigured()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token", DateTimeOffset.UtcNow.AddHours(1));
        connection.SetFolder("folder-123", "Meeting Notes");
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var query = new GetDriveConnectionStatus.Query(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(query);

        // Assert
        Assert.True(result.IsConnected);
        Assert.Equal("Meeting Notes", result.FolderName);
        Assert.Equal("folder-123", result.FolderId);
        Assert.True(result.IsConfigured);
    }

    [Fact]
    public async Task ExecuteAsync_WithConfiguredConnection_ReturnsAllConfigFields()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token", DateTimeOffset.UtcNow.AddHours(1));
        connection.Configure("folder-123", "Meeting Notes", new DateOnly(2026, 1, 1), 30, true);
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var query = new GetDriveConnectionStatus.Query(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(query);

        // Assert
        Assert.True(result.IsConnected);
        Assert.True(result.IsConfigured);
        Assert.Equal("folder-123", result.FolderId);
        Assert.Equal("Meeting Notes", result.FolderName);
        Assert.Equal(new DateOnly(2026, 1, 1), result.InitialImportCutoffDate);
        Assert.Equal(30, result.SyncFrequencyMinutes);
        Assert.True(result.AutoAcceptTags);
    }
}
