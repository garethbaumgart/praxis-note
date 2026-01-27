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

    public async Task AddAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        await context.Meetings.AddAsync(meeting, cancellationToken);
    }

    public void Remove(Meeting meeting)
    {
        context.Meetings.Remove(meeting);
    }
}
