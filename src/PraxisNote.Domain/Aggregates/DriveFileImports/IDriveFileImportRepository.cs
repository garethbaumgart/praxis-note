namespace PraxisNote.Domain.Aggregates.DriveFileImports;

public interface IDriveFileImportRepository
{
    Task<DriveFileImport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DriveFileImport?> GetByDriveFileIdAsync(Guid driveConnectionId, string driveFileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriveFileImport>> GetByConnectionIdAsync(Guid driveConnectionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriveFileImport>> GetByStatusAsync(Guid driveConnectionId, DriveFileImportStatus status, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetExistingDriveFileIdsAsync(Guid driveConnectionId, IEnumerable<string> driveFileIds, CancellationToken cancellationToken = default);
    Task AddAsync(DriveFileImport import, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<DriveFileImport> imports, CancellationToken cancellationToken = default);
    void Remove(DriveFileImport import);
}
