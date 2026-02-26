using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Application.Features.Drive;

public sealed class UpdateDriveSettings(
    IDriveConnectionRepository repository,
    IUnitOfWork unitOfWork)
{
    public record Command(
        Guid UserId,
        Guid ProfileId,
        string FolderId,
        string FolderName,
        DateOnly? InitialImportCutoffDate,
        int SyncFrequencyMinutes,
        bool AutoAcceptTags);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetByUserIdAsync(command.UserId, command.ProfileId, cancellationToken)
            ?? throw new InvalidOperationException("No Google Drive connection found.");

        connection.Configure(
            command.FolderId,
            command.FolderName,
            command.InitialImportCutoffDate,
            command.SyncFrequencyMinutes,
            command.AutoAcceptTags);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
