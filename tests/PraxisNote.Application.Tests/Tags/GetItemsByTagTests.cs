using NSubstitute;
using PraxisNote.Application.Features.Tags;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Tests.Tags;

public class GetItemsByTagTests
{
    private readonly ITagRepository _tagRepo = Substitute.For<ITagRepository>();
    private readonly INoteRepository _noteRepo = Substitute.For<INoteRepository>();
    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly ITaskRepository _taskRepo = Substitute.For<ITaskRepository>();
    private readonly GetItemsByTag _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tagId = Guid.NewGuid();

    public GetItemsByTagTests()
    {
        _sut = new GetItemsByTag(_tagRepo, _noteRepo, _meetingRepo, _taskRepo);
    }

    #region Tag Validation

    [Fact]
    public async Task ExecuteAsync_TagNotFound_ThrowsNotFoundError()
    {
        _tagRepo.GetByIdAsync(_tagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new GetItemsByTag.Query(_userId, _tagId)));

        Assert.Equal(GetItemsByTag.NotFoundError, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TagBelongsToDifferentUser_ThrowsNotFoundError()
    {
        var otherUserId = Guid.NewGuid();
        var tag = Tag.Create(otherUserId, "other-tag");
        _tagRepo.GetByIdAsync(_tagId, Arg.Any<CancellationToken>()).Returns(tag);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(new GetItemsByTag.Query(_userId, _tagId)));

        Assert.Equal(GetItemsByTag.NotFoundError, ex.Message);
    }

    #endregion

    #region Aggregation

    [Fact]
    public async Task ExecuteAsync_WithNoItems_ReturnsEmptyResponse()
    {
        SetupValidTag();
        SetupEmptyRepositories();

        var result = await _sut.ExecuteAsync(new GetItemsByTag.Query(_userId, _tagId));

        Assert.Empty(result.Items);
        Assert.Equal(0, result.MeetingCount);
        Assert.Equal(0, result.NoteCount);
        Assert.Equal(0, result.TaskCount);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithAllItemTypes_ReturnsCombinedItems()
    {
        SetupValidTag();

        var meeting = Meeting.Create(_userId, "Sprint Planning", attendees: "Alice, Bob");
        _meetingRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { meeting });

        var note = Note.Create(_userId);
        _noteRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note });

        var task = TaskItem.CreateStandalone(_userId, "Fix bug");
        _taskRepo.GetTasksWithTagAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });

        var result = await _sut.ExecuteAsync(new GetItemsByTag.Query(_userId, _tagId));

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.MeetingCount);
        Assert.Equal(1, result.NoteCount);
        Assert.Equal(1, result.TaskCount);
        Assert.Contains(result.Items, i => i.Type == "Meeting");
        Assert.Contains(result.Items, i => i.Type == "Note");
        Assert.Contains(result.Items, i => i.Type == "Task");
    }

    [Fact]
    public async Task ExecuteAsync_ItemsSortedByDateDescending()
    {
        SetupValidTag();

        var olderMeeting = Meeting.Create(_userId, "Old Meeting", null);
        var newerNote = Note.Create(_userId);

        _meetingRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { olderMeeting });
        _noteRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { newerNote });
        _taskRepo.GetTasksWithTagAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());

        var result = await _sut.ExecuteAsync(new GetItemsByTag.Query(_userId, _tagId));

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items[0].Date >= result.Items[1].Date);
    }

    #endregion

    #region Meeting Mapping

    [Fact]
    public async Task ExecuteAsync_MeetingWithNullTitle_UsesUntitledMeeting()
    {
        SetupValidTag();
        var meeting = Meeting.Create(_userId, null, null);
        _meetingRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { meeting });
        SetupEmptyNotesAndTasks();

        var result = await _sut.ExecuteAsync(new GetItemsByTag.Query(_userId, _tagId));

        Assert.Equal("Untitled Meeting", result.Items[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_MeetingWithAttendees_CountsCorrectly()
    {
        SetupValidTag();
        var meeting = Meeting.Create(_userId, "Standup", attendees: "Alice, Bob, Charlie");
        _meetingRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting> { meeting });
        SetupEmptyNotesAndTasks();

        var result = await _sut.ExecuteAsync(new GetItemsByTag.Query(_userId, _tagId));

        Assert.Equal(3, result.Items[0].AttendeeCount);
    }

    #endregion

    #region Note Title Extraction

    [Fact]
    public async Task ExecuteAsync_NoteWithEmptyContent_ReturnsUntitled()
    {
        SetupValidTag();
        var note = Note.Create(_userId);
        _noteRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note });
        SetupEmptyMeetingsAndTasks();

        var result = await _sut.ExecuteAsync(new GetItemsByTag.Query(_userId, _tagId));

        Assert.Equal("Untitled", result.Items[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_NoteWithHeading_ExtractsTitle()
    {
        SetupValidTag();
        var note = Note.Create(_userId);
        note.UpdateContent("{\"type\":\"doc\",\"content\":[{\"type\":\"heading\",\"attrs\":{\"level\":1},\"content\":[{\"type\":\"text\",\"text\":\"My Heading\"}]}]}");
        _noteRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note });
        SetupEmptyMeetingsAndTasks();

        var result = await _sut.ExecuteAsync(new GetItemsByTag.Query(_userId, _tagId));

        Assert.Equal("My Heading", result.Items[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_NoteWithLongTitle_TruncatesTo50Chars()
    {
        SetupValidTag();
        var note = Note.Create(_userId);
        var longTitle = new string('A', 60);
        note.UpdateContent($"{{\"type\":\"doc\",\"content\":[{{\"type\":\"heading\",\"attrs\":{{\"level\":1}},\"content\":[{{\"type\":\"text\",\"text\":\"{longTitle}\"}}]}}]}}");
        _noteRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note> { note });
        SetupEmptyMeetingsAndTasks();

        var result = await _sut.ExecuteAsync(new GetItemsByTag.Query(_userId, _tagId));

        Assert.Equal(50, result.Items[0].Title.Length);
        Assert.EndsWith("...", result.Items[0].Title);
    }

    #endregion

    #region Task Mapping

    [Fact]
    public async Task ExecuteAsync_TaskMapsStatusAndPriority()
    {
        SetupValidTag();
        var task = TaskItem.CreateStandalone(_userId, "Important task");
        task.TogglePriority();
        _taskRepo.GetTasksWithTagAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });
        SetupEmptyNotesAndMeetings();

        var result = await _sut.ExecuteAsync(new GetItemsByTag.Query(_userId, _tagId));

        Assert.Equal("Task", result.Items[0].Type);
        Assert.Equal("Todo", result.Items[0].Status);
        Assert.True(result.Items[0].IsPriority);
    }

    #endregion

    #region Helpers

    private void SetupValidTag()
    {
        var tag = Tag.Create(_userId, "test-tag");
        _tagRepo.GetByIdAsync(_tagId, Arg.Any<CancellationToken>()).Returns(tag);
    }

    private void SetupEmptyRepositories()
    {
        _noteRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());
        _meetingRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());
        _taskRepo.GetTasksWithTagAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
    }

    private void SetupEmptyNotesAndTasks()
    {
        _noteRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());
        _taskRepo.GetTasksWithTagAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
    }

    private void SetupEmptyMeetingsAndTasks()
    {
        _meetingRepo.GetByTagIdAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());
        _taskRepo.GetTasksWithTagAsync(_userId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
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
