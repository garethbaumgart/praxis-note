using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.CalendarConnections;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Profiles;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Profiles;

public sealed class DeleteProfile(
    IProfileRepository profileRepository,
    ITaskRepository taskRepository,
    INoteRepository noteRepository,
    IMeetingRepository meetingRepository,
    ITagRepository tagRepository,
    ICalendarConnectionRepository calendarConnectionRepository,
    IUnitOfWork unitOfWork)
{
    public const string NotFoundError = "Profile not found";
    public const string CannotDeleteDefaultError = "Cannot delete the default profile";
    public const string HasDataError = "Profile has data. Move or delete data before removing the profile.";

    public record Command(Guid UserId, Guid ProfileId);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var profile = await profileRepository.GetByIdAsync(command.ProfileId, cancellationToken);
        if (profile is null || profile.UserId != command.UserId)
        {
            throw new InvalidOperationException(NotFoundError);
        }

        if (profile.IsDefault)
        {
            throw new InvalidOperationException(CannotDeleteDefaultError);
        }

        // Check if profile has any data across all entity types (lightweight existence checks)
        if (await taskRepository.ExistsByProfileAsync(command.UserId, command.ProfileId, cancellationToken)
            || await noteRepository.ExistsByProfileAsync(command.UserId, command.ProfileId, cancellationToken)
            || await meetingRepository.ExistsByProfileAsync(command.UserId, command.ProfileId, cancellationToken)
            || await tagRepository.ExistsByProfileAsync(command.UserId, command.ProfileId, cancellationToken)
            || await calendarConnectionRepository.ExistsByProfileAsync(command.UserId, command.ProfileId, cancellationToken))
        {
            throw new InvalidOperationException(HasDataError);
        }

        profileRepository.Remove(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
