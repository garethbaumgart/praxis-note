using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Features.Drive.Services;

public interface IDriveDeduplicationService
{
    Task DeduplicateAsync(
        Guid userId,
        Guid profileId,
        IReadOnlyList<DriveFileImport> parsedFiles,
        CancellationToken cancellationToken = default);
}
