using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class DriveConnectionRepository(PraxisNoteDbContext context) : IDriveConnectionRepository
{
    public async Task<DriveConnection?> GetByUserIdAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        return await context.DriveConnections
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProfileId == profileId, cancellationToken);
    }

    public async Task<DriveConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.DriveConnections.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<DriveConnection>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.DriveConnections
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DriveConnection>> GetConnectionsDueForSyncAsync(CancellationToken cancellationToken = default)
    {
        // Load all active connections with a configured folder that aren't paused, then filter in-memory
        var connections = await context.DriveConnections
            .Where(c => c.ConsecutiveFailures < 5)
            .Where(c => c.FolderId != null)
            .Where(c => c.SyncFrequencyMinutes > 0)
            .ToListAsync(cancellationToken);

        return connections.Where(c => c.IsDueForSync()).ToList();
    }

    public async Task AddAsync(DriveConnection connection, CancellationToken cancellationToken = default)
    {
        await context.DriveConnections.AddAsync(connection, cancellationToken);
    }

    public void Remove(DriveConnection connection)
    {
        context.DriveConnections.Remove(connection);
    }
}
