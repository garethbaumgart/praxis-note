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

    public async Task<IReadOnlyList<DriveConnection>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.DriveConnections
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
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
