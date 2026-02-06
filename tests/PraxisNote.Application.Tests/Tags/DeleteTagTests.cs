using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Tags;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Tests.Tags;

public class DeleteTagTests
{
    private readonly ITagRepository _tagRepo = Substitute.For<ITagRepository>();
    private readonly ITaskRepository _taskRepo = Substitute.For<ITaskRepository>();
    private readonly INoteRepository _noteRepo = Substitute.For<INoteRepository>();
    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteTag _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tagId = Guid.NewGuid();

    public DeleteTagTests()
    {
        _sut = new DeleteTag(_tagRepo, _taskRepo, _noteRepo, _meetingRepo, _unitOfWork);
    }

    #region Validation

    [Fact]
    public async Task ExecuteAsync_TagNotFound_ThrowsNotFoundError()
    {
        _tagRepo.GetByIdAsync(_tagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new DeleteTag.Command(_userId, _tagId)));

        Assert.Equal(DeleteTag.NotFoundError, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TagBelongsToDifferentUser_ThrowsNotFoundError()
    {
        var otherUserId = Guid.NewGuid();
        var tag = Tag.Create(otherUserId, "other-tag");
        _tagRepo.GetByIdAsync(_tagId, Arg.Any<CancellationToken>()).Returns(tag);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new DeleteTag.Command(_userId, _tagId)));

        Assert.Equal(DeleteTag.NotFoundError, ex.Message);
    }

    #endregion

    #region Cascading Removal

    [Fact]
    public async Task ExecuteAsync_TagWithTasks_RemovesTagFromAllTasks()
    {
        SetupValidTag();
        SetupEmptyNotesAndMeetings();

        var task1 = TaskItem.CreateStandalone(_userId, "Task 1");
        task1.AddTag(_tagId);
        var task2 = TaskItem.CreateStandalone(_userId, "Task 2");
        task2.AddTag(_tagId);

        _taskRepo.GetTasksWithTagAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task1, task2 });

        await _sut.ExecuteAsync(new DeleteTag.Command(_userId, _tagId));

        Assert.DoesNotContain(_tagId, task1.TagIds);
        Assert.DoesNotContain(_tagId, task2.TagIds);
    }

    [Fact]
    public async Task ExecuteAsync_TagWithNotes_RemovesTagFromAllNotes()
    {
        SetupValidTag();
        SetupEmptyTasksAndMeetings();

        var note1 = Note.Create(_userId);
        note1.AddTag(_tagId);
        var note2 = Note.Create(_userId);
        note2.AddTag(_tagId);

        _noteRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note1, note2 });

        await _sut.ExecuteAsync(new DeleteTag.Command(_userId, _tagId));

        Assert.DoesNotContain(_tagId, note1.TagIds);
        Assert.DoesNotContain(_tagId, note2.TagIds);
    }

    [Fact]
    public async Task ExecuteAsync_TagWithMeetings_RemovesTagFromAllMeetings()
    {
        SetupValidTag();
        SetupEmptyTasksAndNotes();

        var meeting1 = Meeting.Create(_userId, "Meeting 1", attendees: "");
        meeting1.AddTag(_tagId);
        var meeting2 = Meeting.Create(_userId, "Meeting 2", attendees: "");
        meeting2.AddTag(_tagId);

        _meetingRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { meeting1, meeting2 });

        await _sut.ExecuteAsync(new DeleteTag.Command(_userId, _tagId));

        Assert.DoesNotContain(_tagId, meeting1.TagIds);
        Assert.DoesNotContain(_tagId, meeting2.TagIds);
    }

    [Fact]
    public async Task ExecuteAsync_TagWithAllEntityTypes_RemovesFromAllAndDeletesTag()
    {
        var tag = SetupValidTag();

        var task = TaskItem.CreateStandalone(_userId, "Task");
        task.AddTag(_tagId);
        _taskRepo.GetTasksWithTagAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });

        var note = Note.Create(_userId);
        note.AddTag(_tagId);
        _noteRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note });

        var meeting = Meeting.Create(_userId, "Meeting", attendees: "");
        meeting.AddTag(_tagId);
        _meetingRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { meeting });

        await _sut.ExecuteAsync(new DeleteTag.Command(_userId, _tagId));

        Assert.DoesNotContain(_tagId, task.TagIds);
        Assert.DoesNotContain(_tagId, note.TagIds);
        Assert.DoesNotContain(_tagId, meeting.TagIds);
        _tagRepo.Received(1).Remove(tag);
    }

    [Fact]
    public async Task ExecuteAsync_TagWithNoItems_DeletesTagOnly()
    {
        var tag = SetupValidTag();
        SetupEmptyRepositories();

        await _sut.ExecuteAsync(new DeleteTag.Command(_userId, _tagId));

        _tagRepo.Received(1).Remove(tag);
    }

    #endregion

    #region Transaction

    [Fact]
    public async Task ExecuteAsync_CallsSaveChangesAsyncExactlyOnce()
    {
        SetupValidTag();
        SetupEmptyRepositories();

        await _sut.ExecuteAsync(new DeleteTag.Command(_userId, _tagId));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_RemovesTagFromRepository()
    {
        var tag = SetupValidTag();
        SetupEmptyRepositories();

        await _sut.ExecuteAsync(new DeleteTag.Command(_userId, _tagId));

        _tagRepo.Received(1).Remove(tag);
    }

    #endregion

    #region Helpers

    private Tag SetupValidTag()
    {
        var tag = Tag.Create(_userId, "test-tag");
        _tagRepo.GetByIdAsync(_tagId, Arg.Any<CancellationToken>()).Returns(tag);
        return tag;
    }

    private void SetupEmptyRepositories()
    {
        _taskRepo.GetTasksWithTagAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
        _noteRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());
        _meetingRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());
    }

    private void SetupEmptyTasksAndNotes()
    {
        _taskRepo.GetTasksWithTagAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
        _noteRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());
    }

    private void SetupEmptyTasksAndMeetings()
    {
        _taskRepo.GetTasksWithTagAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
        _meetingRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());
    }

    private void SetupEmptyNotesAndMeetings()
    {
        _noteRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());
        _meetingRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());
    }

    #endregion
}
