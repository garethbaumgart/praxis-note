using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.JiraConnections;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class JiraConnectionRepository(PraxisNoteDbContext context) : IJiraConnectionRepository
{
    public async Task<JiraConnection?> GetByUserIdAndProfileAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        return await context.JiraConnections
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProfileId == profileId, cancellationToken);
    }

    public async Task<IReadOnlyList<JiraConnection>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.JiraConnections
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(JiraConnection connection, CancellationToken cancellationToken = default)
    {
        await context.JiraConnections.AddAsync(connection, cancellationToken);
    }

    public void Remove(JiraConnection connection)
    {
        context.JiraConnections.Remove(connection);
    }
}
