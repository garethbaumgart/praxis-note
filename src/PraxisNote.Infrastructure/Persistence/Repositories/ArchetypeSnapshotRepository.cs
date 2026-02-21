using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.ArchetypeSnapshots;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class ArchetypeSnapshotRepository(PraxisNoteDbContext context) : IArchetypeSnapshotRepository
{
    public async Task<IReadOnlyList<ArchetypeSnapshot>> GetByUserIdAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        return await context.ArchetypeSnapshots
            .Where(s => s.UserId == userId && s.ProfileId == profileId)
            .OrderBy(s => s.WeekStartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ArchetypeSnapshot?> GetByWeekAsync(
        Guid userId,
        Guid profileId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default)
    {
        return await context.ArchetypeSnapshots
            .FirstOrDefaultAsync(
                s => s.UserId == userId && s.ProfileId == profileId && s.WeekStartDate == weekStartDate,
                cancellationToken);
    }

    public async Task AddAsync(ArchetypeSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await context.ArchetypeSnapshots.AddAsync(snapshot, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
