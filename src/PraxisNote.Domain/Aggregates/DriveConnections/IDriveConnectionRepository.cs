namespace PraxisNote.Domain.Aggregates.DriveConnections;

public interface IDriveConnectionRepository
{
    Task<DriveConnection?> GetByUserIdAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriveConnection>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(DriveConnection connection, CancellationToken cancellationToken = default);
    void Remove(DriveConnection connection);
}
