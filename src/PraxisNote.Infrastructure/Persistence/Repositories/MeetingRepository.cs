using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class MeetingRepository(PraxisNoteDbContext context) : IMeetingRepository
{
    public async Task<Meeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Meetings.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Meeting>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Meetings
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.MeetingDate ?? m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Meeting>> GetByTagIdAsync(Guid userId, Guid tagId, CancellationToken cancellationToken = default)
    {
        // In-memory filtering required because TagIds uses a JSON value conversion
        // that EF Core can't translate Contains() on. Same pattern as GetTagUsageCountsAsync.
        var meetings = await context.Meetings
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);

        return meetings.Where(m => m.TagIds.Contains(tagId)).ToList();
    }

    public async Task AddAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        await context.Meetings.AddAsync(meeting, cancellationToken);
    }

    public void Remove(Meeting meeting)
    {
        context.Meetings.Remove(meeting);
    }

    public async Task<HashSet<string>> GetExistingCalendarEventIdsAsync(Guid userId, IEnumerable<string> eventIds, CancellationToken cancellationToken = default)
    {
        var eventIdList = eventIds.ToList();
        var existing = await context.Meetings
            .Where(m => m.UserId == userId && m.CalendarEventId != null && eventIdList.Contains(m.CalendarEventId))
            .Select(m => m.CalendarEventId!)
            .ToListAsync(cancellationToken);

        return existing.ToHashSet();
    }
}
