using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive.Services;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Features.Drive;

public sealed class DeduplicateDriveFiles(
    IDriveConnectionRepository connectionRepository,
    IDriveFileImportRepository fileImportRepository,
    IDriveDeduplicationService deduplicationService,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid ProfileId);
    public record Result(int Checked, int DefiniteDuplicates, int PossibleDuplicates);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByUserIdAsync(command.UserId, command.ProfileId, cancellationToken)
            ?? throw new InvalidOperationException("No Google Drive connection found.");

        var parsedFiles = await fileImportRepository.GetByStatusAsync(
            connection.Id, DriveFileImportStatus.Parsed, cancellationToken);

        if (parsedFiles.Count == 0)
            return new Result(0, 0, 0);

        await deduplicationService.DeduplicateAsync(
            command.UserId, command.ProfileId, parsedFiles, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var definite = parsedFiles.Count(f => f.DuplicateType is DeduplicationType.ExactFile or DeduplicationType.CalendarEvent);
        var possible = parsedFiles.Count(f => f.DuplicateType == DeduplicationType.FuzzyMatch);

        return new Result(parsedFiles.Count, definite, possible);
    }
}
