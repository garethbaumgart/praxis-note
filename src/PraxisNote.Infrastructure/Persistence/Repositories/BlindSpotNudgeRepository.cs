using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.BlindSpotNudges;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class BlindSpotNudgeRepository(PraxisNoteDbContext context) : IBlindSpotNudgeRepository
{
    public async Task<IReadOnlyList<BlindSpotNudge>> GetActiveByUserAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        return await context.BlindSpotNudges
            .Where(n => n.UserId == userId && n.ProfileId == profileId && n.Status == NudgeStatus.Active)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<BlindSpotNudge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.BlindSpotNudges.FindAsync([id], cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<BlindSpotNudge> nudges, CancellationToken cancellationToken = default)
    {
        await context.BlindSpotNudges.AddRangeAsync(nudges, cancellationToken);
    }

    public void Remove(BlindSpotNudge nudge)
    {
        context.BlindSpotNudges.Remove(nudge);
    }
}
