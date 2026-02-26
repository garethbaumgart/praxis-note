using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Features.Drive;

public sealed class SkipDriveImports(
    IDriveFileImportRepository driveFileImportRepository,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, List<Guid> DriveFileImportIds);

    public async Task ExecuteAsync(Command command, CancellationToken ct = default)
    {
        if (command.DriveFileImportIds.Count == 0)
            return;

        foreach (var id in command.DriveFileImportIds)
        {
            var import = await driveFileImportRepository.GetByIdAsync(id, ct);
            if (import is null) continue;

            // Only skip files that are in a skippable state
            if (import.Status is DriveFileImportStatus.Pending or DriveFileImportStatus.Parsed)
            {
                import.MarkSkipped("Skipped by user during import review");
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
