using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Calendar;
using PraxisNote.Domain.Aggregates.CalendarConnections;

namespace PraxisNote.Application.Tests.Calendar;

public class ConnectGoogleCalendarTests
{
    private readonly ICalendarConnectionRepository _repo = Substitute.For<ICalendarConnectionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ConnectGoogleCalendar _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public ConnectGoogleCalendarTests()
    {
        _sut = new ConnectGoogleCalendar(_repo, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingConnection_CreatesNew()
    {
        _repo.GetByUserIdAndProviderAsync(_userId, _profileId, "Google", Arg.Any<CancellationToken>())
            .Returns((CalendarConnection?)null);

        var command = new ConnectGoogleCalendar.Command(
            _userId, _profileId, "access-token", "refresh-token", DateTimeOffset.UtcNow.AddHours(1));

        await _sut.ExecuteAsync(command);

        await _repo.Received(1).AddAsync(Arg.Any<CalendarConnection>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingConnection_RemovesOldAndCreatesNew()
    {
        var existing = CalendarConnection.Create(_userId, _profileId, "Google", "old-access", "old-refresh", DateTimeOffset.UtcNow.AddHours(1));
        _repo.GetByUserIdAndProviderAsync(_userId, _profileId, "Google", Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new ConnectGoogleCalendar.Command(
            _userId, _profileId, "new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(1));

        await _sut.ExecuteAsync(command);

        _repo.Received(1).Remove(existing);
        await _repo.Received(1).AddAsync(Arg.Any<CalendarConnection>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
