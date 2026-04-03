using PraxisNote.Domain.Aggregates.CalendarConnections;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Profiles;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.AccountLinking;

/// <summary>
/// Transfers all data owned by one user to a new profile on another user.
/// Used during account linking to prevent data loss when User B is deleted.
/// </summary>
public class UserDataTransferService(
    ITaskRepository taskRepository,
    INoteRepository noteRepository,
    IMeetingRepository meetingRepository,
    ITagRepository tagRepository,
    ICalendarConnectionRepository calendarConnectionRepository,
    IDriveConnectionRepository driveConnectionRepository,
    IProfileRepository profileRepository)
{
    /// <summary>
    /// Reassigns all data entities from the source user to the target user and profile,
    /// then removes the source user's profiles.
    /// Does not call SaveChangesAsync - the caller is responsible for persisting changes.
    /// </summary>
    /// <param name="sourceUserId">The user whose data will be transferred.</param>
    /// <param name="targetUserId">The user who will own the data after transfer.</param>
    /// <param name="targetProfileId">The profile the data will belong to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public virtual async Task TransferAsync(
        Guid sourceUserId,
        Guid targetUserId,
        Guid targetProfileId,
        CancellationToken cancellationToken = default)
    {
        // Load all entities sequentially - DbContext is not thread-safe
        var tasks = await taskRepository.GetAllByUserIdAsync(sourceUserId, cancellationToken);
        var notes = await noteRepository.GetAllByUserIdAsync(sourceUserId, cancellationToken);
        var meetings = await meetingRepository.GetAllByUserIdAsync(sourceUserId, cancellationToken);
        var tags = await tagRepository.GetAllByUserIdAsync(sourceUserId, cancellationToken);
        var calendarConnections = await calendarConnectionRepository.GetAllByUserIdAsync(sourceUserId, cancellationToken);
        var driveConnections = await driveConnectionRepository.GetAllByUserIdAsync(sourceUserId, cancellationToken);

        // Reassign all entities to the target user and profile
        foreach (var task in tasks)
            task.Reassign(targetUserId, targetProfileId);

        foreach (var note in notes)
            note.Reassign(targetUserId, targetProfileId);

        foreach (var meeting in meetings)
            meeting.Reassign(targetUserId, targetProfileId);

        foreach (var tag in tags)
            tag.Reassign(targetUserId, targetProfileId);

        foreach (var connection in calendarConnections)
            connection.Reassign(targetUserId, targetProfileId);

        foreach (var driveConnection in driveConnections)
            driveConnection.Reassign(targetUserId, targetProfileId);

        // Remove source user's profiles
        var sourceProfiles = await profileRepository.GetByUserIdAsync(sourceUserId, cancellationToken);
        foreach (var profile in sourceProfiles)
            profileRepository.Remove(profile);
    }
}
