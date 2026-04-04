using NSubstitute;
using PraxisNote.Application.Features.AccountLinking;
using PraxisNote.Domain.Aggregates.CalendarConnections;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Profiles;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Tests.AccountLinking;

public class UserDataTransferServiceTests
{
    private readonly ITaskRepository _taskRepo = Substitute.For<ITaskRepository>();
    private readonly INoteRepository _noteRepo = Substitute.For<INoteRepository>();
    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly ITagRepository _tagRepo = Substitute.For<ITagRepository>();
    private readonly ICalendarConnectionRepository _calendarRepo = Substitute.For<ICalendarConnectionRepository>();
    private readonly IDriveConnectionRepository _driveRepo = Substitute.For<IDriveConnectionRepository>();
    private readonly IProfileRepository _profileRepo = Substitute.For<IProfileRepository>();
    private readonly UserDataTransferService _sut;

    private readonly Guid _sourceUserId = Guid.NewGuid();
    private readonly Guid _sourceProfileId = Guid.NewGuid();
    private readonly Guid _targetUserId = Guid.NewGuid();
    private readonly Guid _targetProfileId = Guid.NewGuid();

    public UserDataTransferServiceTests()
    {
        _sut = new UserDataTransferService(
            _taskRepo, _noteRepo, _meetingRepo, _tagRepo,
            _calendarRepo, _driveRepo, _profileRepo);
    }

    #region Transfer All Entity Types

    [Fact]
    public async Task TransferAsync_ReassignsAllEntityTypes()
    {
        // Arrange
        var task = TaskItem.CreateStandalone(_sourceUserId, _sourceProfileId, "Test task");
        var note = Note.Create(_sourceUserId, _sourceProfileId);
        var meeting = Meeting.Create(_sourceUserId, _sourceProfileId, "Test meeting");
        var tag = Tag.Create(_sourceUserId, _sourceProfileId, "test-tag");
        var calendarConnection = CalendarConnection.Create(
            _sourceUserId, _sourceProfileId, "Google", "access", "refresh", DateTimeOffset.UtcNow.AddHours(1));
        var driveConnection = DriveConnection.Create(
            _sourceUserId, _sourceProfileId, "Google", "drive-access", "drive-refresh", DateTimeOffset.UtcNow.AddHours(1));

        _taskRepo.GetAllByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });
        _noteRepo.GetAllByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note });
        _meetingRepo.GetAllByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { meeting });
        _tagRepo.GetAllByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<Tag> { tag });
        _calendarRepo.GetAllByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<CalendarConnection> { calendarConnection });
        _driveRepo.GetAllByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<DriveConnection> { driveConnection });

        var sourceProfile = Profile.Create(_sourceUserId, "Source Profile");
        _profileRepo.GetByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<Profile> { sourceProfile });

        // Act
        await _sut.TransferAsync(_sourceUserId, _targetUserId, _targetProfileId);

        // Assert - all entities reassigned
        Assert.Equal(_targetUserId, task.UserId);
        Assert.Equal(_targetProfileId, task.ProfileId);

        Assert.Equal(_targetUserId, note.UserId);
        Assert.Equal(_targetProfileId, note.ProfileId);

        Assert.Equal(_targetUserId, meeting.UserId);
        Assert.Equal(_targetProfileId, meeting.ProfileId);

        Assert.Equal(_targetUserId, tag.UserId);
        Assert.Equal(_targetProfileId, tag.ProfileId);

        Assert.Equal(_targetUserId, calendarConnection.UserId);
        Assert.Equal(_targetProfileId, calendarConnection.ProfileId);

        Assert.Equal(_targetUserId, driveConnection.UserId);
        Assert.Equal(_targetProfileId, driveConnection.ProfileId);
    }

    #endregion

    #region Source Profiles Removed

    [Fact]
    public async Task TransferAsync_RemovesSourceProfiles()
    {
        // Arrange
        SetupEmptyRepositories();

        var profile1 = Profile.Create(_sourceUserId, "Profile 1");
        var profile2 = Profile.Create(_sourceUserId, "Profile 2");
        _profileRepo.GetByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<Profile> { profile1, profile2 });

        // Act
        await _sut.TransferAsync(_sourceUserId, _targetUserId, _targetProfileId);

        // Assert
        _profileRepo.Received(1).Remove(profile1);
        _profileRepo.Received(1).Remove(profile2);
    }

    #endregion

    #region No Data - Completes Without Error

    [Fact]
    public async Task TransferAsync_WithNoData_CompletesSuccessfully()
    {
        // Arrange
        SetupEmptyRepositories();

        _profileRepo.GetByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<Profile>());

        // Act & Assert - should not throw
        await _sut.TransferAsync(_sourceUserId, _targetUserId, _targetProfileId);
    }

    #endregion

    #region Helpers

    private void SetupEmptyRepositories()
    {
        _taskRepo.GetAllByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
        _noteRepo.GetAllByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());
        _meetingRepo.GetAllByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());
        _tagRepo.GetAllByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<Tag>());
        _calendarRepo.GetAllByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<CalendarConnection>());
        _driveRepo.GetAllByUserIdAsync(_sourceUserId, Arg.Any<CancellationToken>())
            .Returns(new List<DriveConnection>());
    }

    #endregion
}
