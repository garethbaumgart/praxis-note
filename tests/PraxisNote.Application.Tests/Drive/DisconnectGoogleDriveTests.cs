using Microsoft.Extensions.Logging;
using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Application.Tests.Drive;

public class DisconnectGoogleDriveTests
{
    private readonly IDriveConnectionRepository _repository = Substitute.For<IDriveConnectionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly ILogger<DisconnectGoogleDrive> _logger = Substitute.For<ILogger<DisconnectGoogleDrive>>();
    private readonly DisconnectGoogleDrive _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public DisconnectGoogleDriveTests()
    {
        _sut = new DisconnectGoogleDrive(_repository, _unitOfWork, _httpClientFactory, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_NoExistingConnection_DoesNothing()
    {
        // Arrange
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns((DriveConnection?)null);

        var command = new DisconnectGoogleDrive.Command(_userId, _profileId);

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        _repository.DidNotReceive().Remove(Arg.Any<DriveConnection>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ExistingConnection_RemovesAndSaves()
    {
        // Arrange
        var existing = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token", DateTimeOffset.UtcNow.AddHours(1));
        _repository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(existing);

        // Mock HttpClient for token revocation (best-effort, we don't care if it fails)
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        _httpClientFactory.CreateClient().Returns(httpClient);

        var command = new DisconnectGoogleDrive.Command(_userId, _profileId);

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        _repository.Received(1).Remove(existing);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
