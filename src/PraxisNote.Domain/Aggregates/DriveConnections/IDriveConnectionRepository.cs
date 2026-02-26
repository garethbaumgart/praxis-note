namespace PraxisNote.Domain.Aggregates.DriveConnections;

public interface IDriveConnectionRepository
{
    Task<DriveConnection?> GetByUserIdAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default);
    Task<DriveConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriveConnection>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Gets all active connections due for sync (not paused, folder configured, frequency met).</summary>
    Task<IReadOnlyList<DriveConnection>> GetConnectionsDueForSyncAsync(CancellationToken cancellationToken = default);

    Task AddAsync(DriveConnection connection, CancellationToken cancellationToken = default);
    void Remove(DriveConnection connection);
}
