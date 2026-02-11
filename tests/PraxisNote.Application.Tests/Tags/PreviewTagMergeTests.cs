using NSubstitute;
using PraxisNote.Application.Features.Tags;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Tests.Tags;

public class PreviewTagMergeTests
{
    private readonly ITagRepository _tagRepo = Substitute.For<ITagRepository>();
    private readonly ITaskRepository _taskRepo = Substitute.For<ITaskRepository>();
    private readonly INoteRepository _noteRepo = Substitute.For<INoteRepository>();
    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly PreviewTagMerge _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();
    private readonly Guid _sourceTagId = Guid.NewGuid();
    private readonly Guid _targetTagId = Guid.NewGuid();

    public PreviewTagMergeTests()
    {
        _sut = new PreviewTagMerge(_tagRepo, _taskRepo, _noteRepo, _meetingRepo);
    }

    #region Validation

    [Fact]
    public async Task ExecuteAsync_SameSourceAndTarget_ThrowsSameTagError()
    {
        var tagId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new PreviewTagMerge.Query(_userId, tagId, tagId)));

        Assert.Equal(PreviewTagMerge.SameTagError, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_SourceTagNotFound_ThrowsSourceNotFoundError()
    {
        _tagRepo.GetByIdAsync(_sourceTagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new PreviewTagMerge.Query(_userId, _sourceTagId, _targetTagId)));

        Assert.Equal(PreviewTagMerge.SourceNotFoundError, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_SourceTagBelongsToDifferentUser_ThrowsSourceNotFoundError()
    {
        var otherUserId = Guid.NewGuid();
        var sourceTag = Tag.Create(otherUserId, _profileId, "source-tag");
        _tagRepo.GetByIdAsync(_sourceTagId, Arg.Any<CancellationToken>()).Returns(sourceTag);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new PreviewTagMerge.Query(_userId, _sourceTagId, _targetTagId)));

        Assert.Equal(PreviewTagMerge.SourceNotFoundError, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TargetTagNotFound_ThrowsTargetNotFoundError()
    {
        SetupSourceTag();
        _tagRepo.GetByIdAsync(_targetTagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new PreviewTagMerge.Query(_userId, _sourceTagId, _targetTagId)));

        Assert.Equal(PreviewTagMerge.TargetNotFoundError, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TargetTagBelongsToDifferentUser_ThrowsTargetNotFoundError()
    {
        SetupSourceTag();
        var otherUserId = Guid.NewGuid();
        var targetTag = Tag.Create(otherUserId, _profileId, "target-tag");
        _tagRepo.GetByIdAsync(_targetTagId, Arg.Any<CancellationToken>()).Returns(targetTag);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new PreviewTagMerge.Query(_userId, _sourceTagId, _targetTagId)));

        Assert.Equal(PreviewTagMerge.TargetNotFoundError, ex.Message);
    }

    #endregion

    #region Preview Calculation

    [Fact]
    public async Task ExecuteAsync_NoOverlap_ReturnsSumOfCounts()
    {
        SetupSourceTag();
        SetupTargetTag();

        // Source: 2 tasks, 1 note, 0 meetings
        var sourceTask1 = TaskItem.CreateStandalone(_userId, _profileId, "ST1");
        var sourceTask2 = TaskItem.CreateStandalone(_userId, _profileId, "ST2");
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { sourceTask1, sourceTask2 });

        var sourceNote = Note.Create(_userId, _profileId);
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { sourceNote });

        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());

        // Target: 1 task, 2 notes, 1 meeting
        var targetTask = TaskItem.CreateStandalone(_userId, _profileId, "TT1");
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _targetTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { targetTask });

        var targetNote1 = Note.Create(_userId, _profileId);
        var targetNote2 = Note.Create(_userId, _profileId);
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _targetTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { targetNote1, targetNote2 });

        var targetMeeting = Meeting.Create(_userId, _profileId, "TM1", attendees: "");
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _targetTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { targetMeeting });

        var result = await _sut.ExecuteAsync(new PreviewTagMerge.Query(_userId, _sourceTagId, _targetTagId));

        Assert.Equal(2, result.SourceTaskCount);
        Assert.Equal(1, result.SourceNoteCount);
        Assert.Equal(0, result.SourceMeetingCount);
        Assert.Equal(1, result.TargetTaskCount);
        Assert.Equal(2, result.TargetNoteCount);
        Assert.Equal(1, result.TargetMeetingCount);
        Assert.Equal(3, result.ResultTaskCount);   // 2 + 1
        Assert.Equal(3, result.ResultNoteCount);   // 1 + 2
        Assert.Equal(1, result.ResultMeetingCount); // 0 + 1
        Assert.Equal(0, result.OverlapCount);
    }

    [Fact]
    public async Task ExecuteAsync_PartialOverlap_ReturnsCorrectResultAndOverlapCount()
    {
        SetupSourceTag();
        SetupTargetTag();

        // Shared task (has both tags)
        var sharedTask = TaskItem.CreateStandalone(_userId, _profileId, "Shared");
        var sourceOnlyTask = TaskItem.CreateStandalone(_userId, _profileId, "Source Only");

        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { sharedTask, sourceOnlyTask });
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _targetTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { sharedTask });

        _noteRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _targetTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());

        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _targetTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());

        var result = await _sut.ExecuteAsync(new PreviewTagMerge.Query(_userId, _sourceTagId, _targetTagId));

        Assert.Equal(2, result.SourceTaskCount);
        Assert.Equal(1, result.TargetTaskCount);
        Assert.Equal(2, result.ResultTaskCount); // 2 + 1 - 1 overlap
        Assert.Equal(1, result.OverlapCount);
    }

    [Fact]
    public async Task ExecuteAsync_FullOverlap_AllItemsHaveBothTags_ReturnCorrectCounts()
    {
        SetupSourceTag();
        SetupTargetTag();

        var task = TaskItem.CreateStandalone(_userId, _profileId, "Task");
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _targetTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });

        var note = Note.Create(_userId, _profileId);
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note });
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _targetTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note });

        var meeting = Meeting.Create(_userId, _profileId, "Meeting", attendees: "");
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { meeting });
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _targetTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { meeting });

        var result = await _sut.ExecuteAsync(new PreviewTagMerge.Query(_userId, _sourceTagId, _targetTagId));

        Assert.Equal(1, result.ResultTaskCount);    // 1 + 1 - 1 overlap
        Assert.Equal(1, result.ResultNoteCount);    // 1 + 1 - 1 overlap
        Assert.Equal(1, result.ResultMeetingCount); // 1 + 1 - 1 overlap
        Assert.Equal(3, result.OverlapCount);
    }

    [Fact]
    public async Task ExecuteAsync_SourceHasNoItems_ReturnsTargetCountsUnchanged()
    {
        SetupSourceTag();
        SetupTargetTag();

        // Source: empty
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _sourceTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());

        // Target: 2 tasks, 1 note, 1 meeting
        var targetTask1 = TaskItem.CreateStandalone(_userId, _profileId, "TT1");
        var targetTask2 = TaskItem.CreateStandalone(_userId, _profileId, "TT2");
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _targetTagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { targetTask1, targetTask2 });

        var targetNote = Note.Create(_userId, _profileId);
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _targetTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { targetNote });

        var targetMeeting = Meeting.Create(_userId, _profileId, "TM1", attendees: "");
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _targetTagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { targetMeeting });

        var result = await _sut.ExecuteAsync(new PreviewTagMerge.Query(_userId, _sourceTagId, _targetTagId));

        Assert.Equal(2, result.ResultTaskCount);
        Assert.Equal(1, result.ResultNoteCount);
        Assert.Equal(1, result.ResultMeetingCount);
        Assert.Equal(0, result.OverlapCount);
        Assert.Equal("source-tag", result.SourceTagName);
        Assert.Equal("target-tag", result.TargetTagName);
    }

    #endregion

    #region Helpers

    private void SetupSourceTag()
    {
        var tag = Tag.Create(_userId, _profileId, "source-tag");
        _tagRepo.GetByIdAsync(_sourceTagId, Arg.Any<CancellationToken>()).Returns(tag);
    }

    private void SetupTargetTag()
    {
        var tag = Tag.Create(_userId, _profileId, "target-tag");
        _tagRepo.GetByIdAsync(_targetTagId, Arg.Any<CancellationToken>()).Returns(tag);
    }

    #endregion
}
