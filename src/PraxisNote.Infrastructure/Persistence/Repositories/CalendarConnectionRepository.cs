using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.CalendarConnections;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class CalendarConnectionRepository(PraxisNoteDbContext context) : ICalendarConnectionRepository
{
    public async Task<CalendarConnection?> GetByUserIdAndProviderAsync(Guid userId, Guid profileId, string provider, CancellationToken cancellationToken = default)
    {
        return await context.CalendarConnections
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProfileId == profileId && c.Provider == provider, cancellationToken);
    }

    public async Task<bool> ExistsByProfileAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        return await context.CalendarConnections
            .AnyAsync(c => c.UserId == userId && c.ProfileId == profileId, cancellationToken);
    }

    public async Task AddAsync(CalendarConnection connection, CancellationToken cancellationToken = default)
    {
        await context.CalendarConnections.AddAsync(connection, cancellationToken);
    }

    public void Remove(CalendarConnection connection)
    {
        context.CalendarConnections.Remove(connection);
    }
}
