using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Tags;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Tests.Tags;

public class MergeTagsTests
{
    private readonly ITagRepository _tagRepo = Substitute.For<ITagRepository>();
    private readonly ITaskRepository _taskRepo = Substitute.For<ITaskRepository>();
    private readonly INoteRepository _noteRepo = Substitute.For<INoteRepository>();
    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly MergeTags _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();
    private readonly Guid _sourceTagId = Guid.NewGuid();
    private readonly Guid _targetTagId = Guid.NewGuid();

    public MergeTagsTests()
    {
        _sut = new MergeTags(_tagRepo, _taskRepo, _noteRepo, _meetingRepo, _unitOfWork);
    }

    #region Validation

    [Fact]
    public async Task ExecuteAsync_SameSourceAndTarget_ThrowsSameTagError()
    {
        var tagId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new MergeTags.Command(_userId, tagId, tagId)));

        Assert.Equal(MergeTags.SameTagError, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_SourceTagNotFound_ThrowsSourceNotFoundError()
    {
        _tagRepo.GetByIdAsync(_sourceTagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId)));

        Assert.Equal(MergeTags.SourceNotFoundError, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_SourceTagBelongsToDifferentUser_ThrowsSourceNotFoundError()
    {
        var otherUserId = Guid.NewGuid();
        var sourceTag = Tag.Create(otherUserId, _profileId, "source-tag");
        _tagRepo.GetByIdAsync(_sourceTagId, Arg.Any<CancellationToken>()).Returns(sourceTag);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId)));

        Assert.Equal(MergeTags.SourceNotFoundError, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TargetTagNotFound_ThrowsTargetNotFoundError()
    {
        SetupSourceTag();
        _tagRepo.GetByIdAsync(_targetTagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId)));

        Assert.Equal(MergeTags.TargetNotFoundError, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TargetTagBelongsToDifferentUser_ThrowsTargetNotFoundError()
    {
        SetupSourceTag();
        var otherUserId = Guid.NewGuid();
        var targetTag = Tag.Create(otherUserId, _profileId, "target-tag");
        _tagRepo.GetByIdAsync(_targetTagId, Arg.Any<CancellationToken>()).Returns(targetTag);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId)));

        Assert.Equal(MergeTags.TargetNotFoundError, ex.Message);
    }

    #endregion

    #region Merge Logic — No Overlap

    [Fact]
    public async Task ExecuteAsync_WithTasksOnly_AddsTargetTagAndRemovesSourceTag()
    {
        SetupSourceTag();
        SetupTargetTag();
        SetupEmptyNotesAndMeetings();

        var task = TaskItem.CreateStandalone(_userId, _profileId, "Task 1");
        task.AddTag(_sourceTagId);
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });

        await _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId));

        Assert.Contains(_targetTagId, task.TagIds);
        Assert.DoesNotContain(_sourceTagId, task.TagIds);
    }

    [Fact]
    public async Task ExecuteAsync_WithNotesOnly_AddsTargetTagAndRemovesSourceTag()
    {
        SetupSourceTag();
        SetupTargetTag();
        SetupEmptyTasksAndMeetings();

        var note = Note.Create(_userId, _profileId);
        note.AddTag(_sourceTagId);
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note });

        await _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId));

        Assert.Contains(_targetTagId, note.TagIds);
        Assert.DoesNotContain(_sourceTagId, note.TagIds);
    }

    [Fact]
    public async Task ExecuteAsync_WithMeetingsOnly_AddsTargetTagAndRemovesSourceTag()
    {
        SetupSourceTag();
        SetupTargetTag();
        SetupEmptyTasksAndNotes();

        var meeting = Meeting.Create(_userId, _profileId, "Meeting 1", attendees: "");
        meeting.AddTag(_sourceTagId);
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { meeting });

        await _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId));

        Assert.Contains(_targetTagId, meeting.TagIds);
        Assert.DoesNotContain(_sourceTagId, meeting.TagIds);
    }

    [Fact]
    public async Task ExecuteAsync_WithAllTypes_MergesAllAndDeletesSourceTag()
    {
        var sourceTag = SetupSourceTag();
        SetupTargetTag();

        var task = TaskItem.CreateStandalone(_userId, _profileId, "Task");
        task.AddTag(_sourceTagId);
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });

        var note = Note.Create(_userId, _profileId);
        note.AddTag(_sourceTagId);
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note });

        var meeting = Meeting.Create(_userId, _profileId, "Meeting", attendees: "");
        meeting.AddTag(_sourceTagId);
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { meeting });

        await _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId));

        Assert.Contains(_targetTagId, task.TagIds);
        Assert.DoesNotContain(_sourceTagId, task.TagIds);
        Assert.Contains(_targetTagId, note.TagIds);
        Assert.DoesNotContain(_sourceTagId, note.TagIds);
        Assert.Contains(_targetTagId, meeting.TagIds);
        Assert.DoesNotContain(_sourceTagId, meeting.TagIds);
        _tagRepo.Received(1).Remove(sourceTag);
    }

    #endregion

    #region Merge Logic — With Overlap

    [Fact]
    public async Task ExecuteAsync_TasksWithBothTags_DoesNotDuplicateTargetTag()
    {
        SetupSourceTag();
        SetupTargetTag();
        SetupEmptyNotesAndMeetings();

        // Task already has both source and target tags
        var task = TaskItem.CreateStandalone(_userId, _profileId, "Overlapping Task");
        task.AddTag(_sourceTagId);
        task.AddTag(_targetTagId);
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });

        await _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId));

        Assert.Contains(_targetTagId, task.TagIds);
        Assert.DoesNotContain(_sourceTagId, task.TagIds);
        // Only one instance of target tag (AddTag is idempotent)
        Assert.Single(task.TagIds, id => id == _targetTagId);
    }

    [Fact]
    public async Task ExecuteAsync_AllItemsOverlap_ReturnsCorrectCounts()
    {
        SetupSourceTag();
        SetupTargetTag();

        var task = TaskItem.CreateStandalone(_userId, _profileId, "Task");
        task.AddTag(_sourceTagId);
        task.AddTag(_targetTagId);
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });

        var note = Note.Create(_userId, _profileId);
        note.AddTag(_sourceTagId);
        note.AddTag(_targetTagId);
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note });

        var meeting = Meeting.Create(_userId, _profileId, "Meeting", attendees: "");
        meeting.AddTag(_sourceTagId);
        meeting.AddTag(_targetTagId);
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { meeting });

        var result = await _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId));

        Assert.Equal(1, result.TaskCount);
        Assert.Equal(1, result.NoteCount);
        Assert.Equal(1, result.MeetingCount);
        Assert.Equal(3, result.TotalCount);
    }

    #endregion

    #region Merge Logic — Edge Cases

    [Fact]
    public async Task ExecuteAsync_SourceTagHasNoItems_DeletesSourceTagOnly()
    {
        var sourceTag = SetupSourceTag();
        SetupTargetTag();
        SetupEmptyRepositories();

        await _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId));

        _tagRepo.Received(1).Remove(sourceTag);
    }

    [Fact]
    public async Task ExecuteAsync_CallsSaveChangesAsyncExactlyOnce()
    {
        SetupSourceTag();
        SetupTargetTag();
        SetupEmptyRepositories();

        await _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCorrectCounts()
    {
        SetupSourceTag();
        SetupTargetTag();

        var task1 = TaskItem.CreateStandalone(_userId, _profileId, "Task 1");
        task1.AddTag(_sourceTagId);
        var task2 = TaskItem.CreateStandalone(_userId, _profileId, "Task 2");
        task2.AddTag(_sourceTagId);
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task1, task2 });

        var note = Note.Create(_userId, _profileId);
        note.AddTag(_sourceTagId);
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note });

        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());

        var result = await _sut.ExecuteAsync(new MergeTags.Command(_userId, _sourceTagId, _targetTagId));

        Assert.Equal(2, result.TaskCount);
        Assert.Equal(1, result.NoteCount);
        Assert.Equal(0, result.MeetingCount);
        Assert.Equal(3, result.TotalCount);
    }

    #endregion

    #region Helpers

    private Tag SetupSourceTag()
    {
        var tag = Tag.Create(_userId, _profileId, "source-tag");
        _tagRepo.GetByIdAsync(_sourceTagId, Arg.Any<CancellationToken>()).Returns(tag);
        return tag;
    }

    private Tag SetupTargetTag()
    {
        var tag = Tag.Create(_userId, _profileId, "target-tag");
        _tagRepo.GetByIdAsync(_targetTagId, Arg.Any<CancellationToken>()).Returns(tag);
        return tag;
    }

    private void SetupEmptyRepositories()
    {
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());
    }

    private void SetupEmptyNotesAndMeetings()
    {
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());
    }

    private void SetupEmptyTasksAndMeetings()
    {
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());
    }

    private void SetupEmptyTasksAndNotes()
    {
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());
    }

    #endregion
}
