using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class MeetingRepository(PraxisNoteDbContext context) : IMeetingRepository
{
    public async Task<Meeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Meetings.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Meeting>> GetByUserIdAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        return await context.Meetings
            .Where(m => m.UserId == userId && m.ProfileId == profileId)
            .OrderByDescending(m => m.MeetingDate ?? m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Meeting>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Meetings
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Meeting>> GetByTagIdAsync(Guid userId, Guid profileId, Guid tagId, CancellationToken cancellationToken = default)
    {
        // In-memory filtering required because TagIds uses a JSON value conversion
        // that EF Core can't translate Contains() on. Same pattern as GetTagUsageCountsAsync.
        var meetings = await context.Meetings
            .Where(m => m.UserId == userId && m.ProfileId == profileId)
            .ToListAsync(cancellationToken);

        return meetings.Where(m => m.TagIds.Contains(tagId)).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetTagUsageCountsAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        var tagIdLists = await context.Meetings
            .Where(m => m.UserId == userId && m.ProfileId == profileId)
            .Select(m => m.TagIds)
            .ToListAsync(cancellationToken);

        return tagIdLists
            .SelectMany(tagIds => tagIds)
            .GroupBy(tagId => tagId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<bool> ExistsByProfileAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        return await context.Meetings
            .AnyAsync(m => m.UserId == userId && m.ProfileId == profileId, cancellationToken);
    }

    public async Task AddAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        await context.Meetings.AddAsync(meeting, cancellationToken);
    }

    public void Remove(Meeting meeting)
    {
        context.Meetings.Remove(meeting);
    }

    public async Task<HashSet<string>> GetExistingCalendarEventIdsAsync(Guid userId, Guid profileId, IEnumerable<string> eventIds, CancellationToken cancellationToken = default)
    {
        var eventIdList = eventIds.ToList();
        var existing = await context.Meetings
            .Where(m => m.UserId == userId && m.ProfileId == profileId && m.CalendarEventId != null && eventIdList.Contains(m.CalendarEventId))
            .Select(m => m.CalendarEventId!)
            .ToListAsync(cancellationToken);

        return existing.ToHashSet();
    }
}
