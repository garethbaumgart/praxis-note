using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Notes.Services;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Application.Tests.Meetings;

public class MeetingNotesTests
{
    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly INoteRepository _noteRepo = Substitute.For<INoteRepository>();
    private readonly ITagRepository _tagRepo = Substitute.For<ITagRepository>();
    private readonly ITaskRepository _taskRepo = Substitute.For<ITaskRepository>();
    private readonly ICheckboxExtractor _checkboxExtractor = Substitute.For<ICheckboxExtractor>();
    private readonly ICheckboxSyncService _checkboxSyncService = Substitute.For<ICheckboxSyncService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    #region CreateMeetingNote

    [Fact]
    public async Task CreateMeetingNote_CreatesNoteAndLinksToMeeting()
    {
        // Arrange
        var meeting = Meeting.Create(_userId, _profileId, "Sprint Planning");
        var tagId = Guid.NewGuid();
        meeting.AddTag(tagId);

        _meetingRepo.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);

        var sut = new CreateMeetingNote(_meetingRepo, _noteRepo, _unitOfWork);
        var command = new CreateMeetingNote.Command(_userId, meeting.Id, "Some notes");

        // Act
        var result = await sut.ExecuteAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result.NoteId);
        Assert.Equal(result.NoteId, meeting.NoteId);
        await _noteRepo.Received(1).AddAsync(Arg.Is<Note>(n =>
            n.Content == "Some notes" &&
            n.TagIds.Contains(tagId)), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMeetingNote_WhenMeetingAlreadyHasNote_Throws()
    {
        // Arrange
        var meeting = Meeting.Create(_userId, _profileId, "Sprint Planning");
        meeting.LinkNote(Guid.NewGuid());

        _meetingRepo.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);

        var sut = new CreateMeetingNote(_meetingRepo, _noteRepo, _unitOfWork);
        var command = new CreateMeetingNote.Command(_userId, meeting.Id, "Some notes");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(command));
        Assert.Equal(CreateMeetingNote.NoteAlreadyExistsError, ex.Message);
    }

    [Fact]
    public async Task CreateMeetingNote_WhenMeetingNotFound_Throws()
    {
        // Arrange
        _meetingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Meeting?)null);

        var sut = new CreateMeetingNote(_meetingRepo, _noteRepo, _unitOfWork);
        var command = new CreateMeetingNote.Command(_userId, Guid.NewGuid(), "Some notes");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(command));
        Assert.Equal(CreateMeetingNote.MeetingNotFoundError, ex.Message);
    }

    [Fact]
    public async Task CreateMeetingNote_CopiesMeetingTagsToNote()
    {
        // Arrange
        var meeting = Meeting.Create(_userId, _profileId, "Sprint Planning");
        var tag1 = Guid.NewGuid();
        var tag2 = Guid.NewGuid();
        meeting.AddTag(tag1);
        meeting.AddTag(tag2);

        _meetingRepo.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);

        var sut = new CreateMeetingNote(_meetingRepo, _noteRepo, _unitOfWork);
        var command = new CreateMeetingNote.Command(_userId, meeting.Id, "Notes");

        // Act
        await sut.ExecuteAsync(command);

        // Assert
        await _noteRepo.Received(1).AddAsync(Arg.Is<Note>(n =>
            n.TagIds.Contains(tag1) && n.TagIds.Contains(tag2)), Arg.Any<CancellationToken>());
    }

    #endregion

    #region AddTagToMeeting Tag Sync

    [Fact]
    public async Task AddTagToMeeting_SyncsTagToLinkedNote()
    {
        // Arrange
        var meeting = Meeting.Create(_userId, _profileId, "Sprint Planning");
        var noteId = Guid.NewGuid();
        meeting.LinkNote(noteId);
        var note = Note.Create(_userId, _profileId);

        var tag = Tag.Create(_userId, _profileId, "important");
        _meetingRepo.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);
        _tagRepo.GetByIdAsync(tag.Id, Arg.Any<CancellationToken>()).Returns(tag);
        _noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(note);

        var sut = new AddTagToMeeting(_meetingRepo, _tagRepo, _noteRepo, _unitOfWork);
        var command = new AddTagToMeeting.Command(_userId, meeting.Id, tag.Id);

        // Act
        await sut.ExecuteAsync(command);

        // Assert
        Assert.True(meeting.HasTag(tag.Id));
        Assert.True(note.HasTag(tag.Id));
    }

    #endregion

    #region RemoveTagFromMeeting Tag Sync

    [Fact]
    public async Task RemoveTagFromMeeting_SyncsTagRemovalToLinkedNote()
    {
        // Arrange
        var meeting = Meeting.Create(_userId, _profileId, "Sprint Planning");
        var noteId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        meeting.LinkNote(noteId);
        meeting.AddTag(tagId);

        var note = Note.Create(_userId, _profileId);
        note.AddTag(tagId);

        _meetingRepo.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);
        _noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(note);

        var sut = new RemoveTagFromMeeting(_meetingRepo, _noteRepo, _unitOfWork);
        var command = new RemoveTagFromMeeting.Command(_userId, meeting.Id, tagId);

        // Act
        await sut.ExecuteAsync(command);

        // Assert
        Assert.False(meeting.HasTag(tagId));
        Assert.False(note.HasTag(tagId));
    }

    #endregion

    #region DeleteMeeting Cascade Delete

    [Fact]
    public async Task DeleteMeeting_CascadeDeletesLinkedNote()
    {
        // Arrange
        var meeting = Meeting.Create(_userId, _profileId, "Sprint Planning");
        var noteId = Guid.NewGuid();
        meeting.LinkNote(noteId);
        var note = Note.Create(_userId, _profileId);

        _meetingRepo.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);
        _noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(note);

        var sut = new DeleteMeeting(_meetingRepo, _noteRepo, _unitOfWork);
        var command = new DeleteMeeting.Command(meeting.Id, _userId);

        // Act
        var result = await sut.ExecuteAsync(command);

        // Assert
        Assert.True(result);
        _noteRepo.Received(1).Remove(note);
        _meetingRepo.Received(1).Remove(meeting);
    }

    [Fact]
    public async Task DeleteMeeting_WithoutNote_DeletesOnlyMeeting()
    {
        // Arrange
        var meeting = Meeting.Create(_userId, _profileId, "Sprint Planning");

        _meetingRepo.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);

        var sut = new DeleteMeeting(_meetingRepo, _noteRepo, _unitOfWork);
        var command = new DeleteMeeting.Command(meeting.Id, _userId);

        // Act
        var result = await sut.ExecuteAsync(command);

        // Assert
        Assert.True(result);
        _noteRepo.DidNotReceive().Remove(Arg.Any<Note>());
        _meetingRepo.Received(1).Remove(meeting);
    }

    #endregion

    #region UpdateMeetingNote Checkbox Sync

    [Fact]
    public async Task UpdateMeetingNote_ExtractsCheckboxes()
    {
        // Arrange
        var meeting = Meeting.Create(_userId, _profileId, "Sprint Planning");
        var noteId = Guid.NewGuid();
        meeting.LinkNote(noteId);
        var note = Note.Create(_userId, _profileId);
        var content = """{"type":"doc","content":[{"type":"taskList","content":[{"type":"taskItem","attrs":{"checked":false},"content":[{"type":"paragraph","content":[{"type":"text","text":"Task 1"}]}]}]}]}""";

        _meetingRepo.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);
        _noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(note);
        _taskRepo.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>()).Returns(new List<TaskItem>());

        var sut = new UpdateMeetingNote(_meetingRepo, _noteRepo, _taskRepo, _checkboxExtractor, _checkboxSyncService, _unitOfWork);
        var command = new UpdateMeetingNote.Command(_userId, meeting.Id, content);

        // Act
        await sut.ExecuteAsync(command);

        // Assert
        _checkboxExtractor.Received(1).Extract(content);
    }

    [Fact]
    public async Task UpdateMeetingNote_CallsCheckboxSyncService()
    {
        // Arrange
        var meeting = Meeting.Create(_userId, _profileId, "Sprint Planning");
        var noteId = Guid.NewGuid();
        meeting.LinkNote(noteId);
        var note = Note.Create(_userId, _profileId);
        var content = """{"type":"doc","content":[]}""";
        var checkboxes = new List<Checkbox> { new("cb-1", "Task 1", false) };

        _meetingRepo.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);
        _noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(note);
        _checkboxExtractor.Extract(content).Returns(checkboxes);
        _taskRepo.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>()).Returns(new List<TaskItem>());

        var sut = new UpdateMeetingNote(_meetingRepo, _noteRepo, _taskRepo, _checkboxExtractor, _checkboxSyncService, _unitOfWork);
        var command = new UpdateMeetingNote.Command(_userId, meeting.Id, content);

        // Act
        await sut.ExecuteAsync(command);

        // Assert
        _checkboxSyncService.Received(1).SyncCheckboxes(
            note,
            Arg.Is<IReadOnlyList<Checkbox>>(list => list.Count == 1 && list[0].Id == "cb-1"),
            Arg.Any<IEnumerable<TaskItem>>());
    }

    [Fact]
    public async Task UpdateMeetingNote_SyncsLinkedTasks()
    {
        // Arrange
        var meeting = Meeting.Create(_userId, _profileId, "Sprint Planning");
        var noteId = Guid.NewGuid();
        meeting.LinkNote(noteId);
        var note = Note.Create(_userId, _profileId);
        var content = """{"type":"doc","content":[]}""";

        var checkboxRef = new CheckboxRef(noteId, "cb-1");
        var linkedTask = TaskItem.CreateFromCheckbox(_userId, _profileId, "Linked task", checkboxRef);
        var unlinkedTask = TaskItem.CreateStandalone(_userId, _profileId, "Unlinked task");
        var tasks = new List<TaskItem> { linkedTask, unlinkedTask };

        _meetingRepo.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);
        _noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(note);
        _checkboxExtractor.Extract(content).Returns(new List<Checkbox>());
        _taskRepo.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>()).Returns(tasks);

        var sut = new UpdateMeetingNote(_meetingRepo, _noteRepo, _taskRepo, _checkboxExtractor, _checkboxSyncService, _unitOfWork);
        var command = new UpdateMeetingNote.Command(_userId, meeting.Id, content);

        // Act
        await sut.ExecuteAsync(command);

        // Assert
        _checkboxSyncService.Received(1).SyncCheckboxes(
            note,
            Arg.Any<IReadOnlyList<Checkbox>>(),
            Arg.Is<IEnumerable<TaskItem>>(tasks =>
                tasks.Count() == 1 && tasks.First().CheckboxRef!.CheckboxId == "cb-1"));
    }

    [Fact]
    public async Task UpdateMeetingNote_UpdatesNoteContent()
    {
        // Arrange
        var meeting = Meeting.Create(_userId, _profileId, "Sprint Planning");
        var noteId = Guid.NewGuid();
        meeting.LinkNote(noteId);
        var note = Note.Create(_userId, _profileId);
        var content = """{"type":"doc","content":[]}""";

        _meetingRepo.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);
        _noteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(note);
        _checkboxExtractor.Extract(content).Returns(new List<Checkbox>());
        _taskRepo.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>()).Returns(new List<TaskItem>());

        var sut = new UpdateMeetingNote(_meetingRepo, _noteRepo, _taskRepo, _checkboxExtractor, _checkboxSyncService, _unitOfWork);
        var command = new UpdateMeetingNote.Command(_userId, meeting.Id, content);

        // Act
        await sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(content, note.Content);
    }

    #endregion
}
