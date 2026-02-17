namespace PraxisNote.Domain.Aggregates.JiraConnections;

public interface IJiraConnectionRepository
{
    Task<JiraConnection?> GetByUserIdAndProfileAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JiraConnection>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(JiraConnection connection, CancellationToken cancellationToken = default);
    void Remove(JiraConnection connection);
}
