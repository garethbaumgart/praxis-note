using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Tests.Meetings;

public class MeetingNotesTests
{
    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly INoteRepository _noteRepo = Substitute.For<INoteRepository>();
    private readonly ITagRepository _tagRepo = Substitute.For<ITagRepository>();
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
}
