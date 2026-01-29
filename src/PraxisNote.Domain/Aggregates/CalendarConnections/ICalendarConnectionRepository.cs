namespace PraxisNote.Domain.Aggregates.CalendarConnections;

public interface ICalendarConnectionRepository
{
    Task<CalendarConnection?> GetByUserIdAndProviderAsync(Guid userId, string provider, CancellationToken cancellationToken = default);
    Task AddAsync(CalendarConnection connection, CancellationToken cancellationToken = default);
    void Remove(CalendarConnection connection);
}
