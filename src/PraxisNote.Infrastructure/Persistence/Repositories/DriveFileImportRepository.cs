using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class DriveFileImportRepository(PraxisNoteDbContext context) : IDriveFileImportRepository
{
    public async Task<DriveFileImport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.DriveFileImports.FindAsync([id], cancellationToken);
    }

    public async Task<DriveFileImport?> GetByDriveFileIdAsync(Guid driveConnectionId, string driveFileId, CancellationToken cancellationToken = default)
    {
        return await context.DriveFileImports
            .FirstOrDefaultAsync(f => f.DriveConnectionId == driveConnectionId && f.DriveFileId == driveFileId, cancellationToken);
    }

    public async Task<IReadOnlyList<DriveFileImport>> GetByConnectionIdAsync(Guid driveConnectionId, CancellationToken cancellationToken = default)
    {
        return await context.DriveFileImports
            .Where(f => f.DriveConnectionId == driveConnectionId)
            .OrderByDescending(f => f.FileModifiedTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DriveFileImport>> GetByStatusAsync(Guid driveConnectionId, DriveFileImportStatus status, CancellationToken cancellationToken = default)
    {
        return await context.DriveFileImports
            .Where(f => f.DriveConnectionId == driveConnectionId && f.Status == status)
            .OrderByDescending(f => f.FileModifiedTime)
            .ToListAsync(cancellationToken);
    }

    // IMPORTANT: Use List<T>.Contains() — NOT HashSet. See MeetingRepository:70 for pattern.
    public async Task<HashSet<string>> GetExistingDriveFileIdsAsync(Guid driveConnectionId, IEnumerable<string> driveFileIds, CancellationToken cancellationToken = default)
    {
        var idList = driveFileIds.ToList();
        var existing = await context.DriveFileImports
            .Where(f => f.DriveConnectionId == driveConnectionId && idList.Contains(f.DriveFileId))
            .Select(f => f.DriveFileId)
            .ToListAsync(cancellationToken);
        return existing.ToHashSet();
    }

    public async Task<int> GetPendingCountByConnectionAsync(Guid driveConnectionId, CancellationToken cancellationToken = default)
    {
        return await context.DriveFileImports
            .CountAsync(f => f.DriveConnectionId == driveConnectionId && f.Status == DriveFileImportStatus.Parsed, cancellationToken);
    }

    public async Task AddAsync(DriveFileImport import, CancellationToken cancellationToken = default)
    {
        await context.DriveFileImports.AddAsync(import, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<DriveFileImport> imports, CancellationToken cancellationToken = default)
    {
        await context.DriveFileImports.AddRangeAsync(imports, cancellationToken);
    }

    public void Remove(DriveFileImport import)
    {
        context.DriveFileImports.Remove(import);
    }
}
