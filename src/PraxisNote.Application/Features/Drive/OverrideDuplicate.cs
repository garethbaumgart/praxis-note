using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Features.Drive;

public sealed class OverrideDuplicate(
    IDriveConnectionRepository connectionRepository,
    IDriveFileImportRepository fileImportRepository,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid ProfileId, Guid FileImportId);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByUserIdAsync(command.UserId, command.ProfileId, cancellationToken)
            ?? throw new InvalidOperationException("No Google Drive connection found.");

        var fileImport = await fileImportRepository.GetByIdAsync(command.FileImportId, cancellationToken)
            ?? throw new InvalidOperationException("File import not found.");

        if (fileImport.DriveConnectionId != connection.Id)
            throw new InvalidOperationException("File import does not belong to this user's Drive connection.");

        fileImport.ClearDuplicate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
