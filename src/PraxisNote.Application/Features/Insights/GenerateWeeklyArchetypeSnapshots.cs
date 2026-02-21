using PraxisNote.Domain.Aggregates.ArchetypeSnapshots;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Application.Features.Insights;

public sealed class GenerateWeeklyArchetypeSnapshots(
    IArchetypeSnapshotRepository snapshotRepository,
    IMeetingRepository meetingRepository,
    IUserRepository userRepository)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        var weekStartDate = GetCurrentWeekStart();
        var defaultProfileId = Guid.Empty; // Default profile (single profile per user for now)

        foreach (var user in users)
        {
            var existing = await snapshotRepository.GetByWeekAsync(
                user.Id,
                defaultProfileId,
                weekStartDate,
                cancellationToken);

            if (existing is not null) continue;

            var getCommunicationProfile = new GetCommunicationProfile(meetingRepository, snapshotRepository);
            var profile = await getCommunicationProfile.ExecuteAsync(
                new GetCommunicationProfile.Query(user.Id, defaultProfileId, "7d"),
                cancellationToken);

            if (!profile.HasEnoughData) continue;

            var primaryScore = profile.Scores.First(s => s.Name == profile.PrimaryArchetype).Score;

            var snapshot = ArchetypeSnapshot.Create(
                user.Id,
                defaultProfileId,
                weekStartDate,
                profile.PrimaryArchetype,
                primaryScore,
                profile.MeetingCount);

            await snapshotRepository.AddAsync(snapshot, cancellationToken);
        }
    }

    private static DateOnly GetCurrentWeekStart()
    {
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysFromMonday = ((int)now.DayOfWeek - 1 + 7) % 7;
        return now.AddDays(-daysFromMonday);
    }
}
