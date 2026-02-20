using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Notes.Services;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;

namespace PraxisNote.Application.Features.Meetings;

public sealed class CreateMeetingNote(
    IMeetingRepository meetingRepository,
    INoteRepository noteRepository,
    ICheckboxExtractor checkboxExtractor,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid MeetingId, string Content);
    public record Result(Guid NoteId);

    public const string MeetingNotFoundError = "MEETING_NOT_FOUND";
    public const string NoteAlreadyExistsError = "NOTE_ALREADY_EXISTS";

    public async Task<Result> ExecuteAsync(Command command, CancellationToken ct = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, ct);
        if (meeting is null || meeting.UserId != command.UserId)
        {
            throw new InvalidOperationException(MeetingNotFoundError);
        }

        if (meeting.NoteId is not null)
        {
            throw new InvalidOperationException(NoteAlreadyExistsError);
        }

        var note = Note.Create(command.UserId, meeting.ProfileId, command.Content);

        // Extract and add checkboxes from content
        if (!string.IsNullOrEmpty(command.Content))
        {
            var checkboxes = checkboxExtractor.Extract(command.Content);
            foreach (var checkbox in checkboxes)
            {
                note.AddCheckbox(checkbox);
            }
        }

        // Copy all meeting tags to new note
        foreach (var tagId in meeting.TagIds)
        {
            note.AddTag(tagId);
        }

        meeting.LinkNote(note.Id);
        await noteRepository.AddAsync(note, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new Result(note.Id);
    }
}
