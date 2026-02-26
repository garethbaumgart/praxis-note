using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Features.Drive;

public sealed class OverrideDuplicate(
    IDriveFileImportRepository fileImportRepository,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid FileImportId);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var fileImport = await fileImportRepository.GetByIdAsync(command.FileImportId, cancellationToken)
            ?? throw new InvalidOperationException("File import not found.");

        fileImport.ClearDuplicate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
