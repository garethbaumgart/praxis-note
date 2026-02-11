using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Calendar;
using PraxisNote.Domain.Aggregates.CalendarConnections;

namespace PraxisNote.Application.Tests.Calendar;

public class DisconnectGoogleCalendarTests
{
    private readonly ICalendarConnectionRepository _repo = Substitute.For<ICalendarConnectionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DisconnectGoogleCalendar _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public DisconnectGoogleCalendarTests()
    {
        _sut = new DisconnectGoogleCalendar(_repo, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingConnection_RemovesAndSaves()
    {
        var connection = CalendarConnection.Create(_userId, _profileId, "Google", "access", "refresh", DateTimeOffset.UtcNow.AddHours(1));
        _repo.GetByUserIdAndProviderAsync(_userId, _profileId, "Google", Arg.Any<CancellationToken>())
            .Returns(connection);

        await _sut.ExecuteAsync(new DisconnectGoogleCalendar.Command(_userId, _profileId));

        _repo.Received(1).Remove(connection);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNoConnection_DoesNothing()
    {
        _repo.GetByUserIdAndProviderAsync(_userId, _profileId, "Google", Arg.Any<CancellationToken>())
            .Returns((CalendarConnection?)null);

        await _sut.ExecuteAsync(new DisconnectGoogleCalendar.Command(_userId, _profileId));

        _repo.DidNotReceive().Remove(Arg.Any<CalendarConnection>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
