using NSubstitute;
using PraxisNote.Application.Features.Calendar;
using PraxisNote.Domain.Aggregates.CalendarConnections;

namespace PraxisNote.Application.Tests.Calendar;

public class GetCalendarConnectionStatusTests
{
    private readonly ICalendarConnectionRepository _repo = Substitute.For<ICalendarConnectionRepository>();
    private readonly GetCalendarConnectionStatus _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public GetCalendarConnectionStatusTests()
    {
        _sut = new GetCalendarConnectionStatus(_repo);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoConnection_ReturnsDisconnected()
    {
        _repo.GetByUserIdAndProviderAsync(_userId, _profileId, "Google", Arg.Any<CancellationToken>())
            .Returns((CalendarConnection?)null);

        var result = await _sut.ExecuteAsync(new GetCalendarConnectionStatus.Query(_userId, _profileId));

        Assert.False(result.IsConnected);
        Assert.Null(result.Provider);
        Assert.Null(result.ConnectedAt);
        Assert.Null(result.LastSyncedAt);
    }

    [Fact]
    public async Task ExecuteAsync_WithConnection_ReturnsConnectedStatus()
    {
        var connection = CalendarConnection.Create(_userId, _profileId, "Google", "access", "refresh", DateTimeOffset.UtcNow.AddHours(1));
        _repo.GetByUserIdAndProviderAsync(_userId, _profileId, "Google", Arg.Any<CancellationToken>())
            .Returns(connection);

        var result = await _sut.ExecuteAsync(new GetCalendarConnectionStatus.Query(_userId, _profileId));

        Assert.True(result.IsConnected);
        Assert.Equal("Google", result.Provider);
        Assert.NotNull(result.ConnectedAt);
        Assert.Null(result.LastSyncedAt);
    }
}
