using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Features.Drive;

public sealed class GetDriveFileImports(
    IDriveConnectionRepository connectionRepository,
    IDriveFileImportRepository fileImportRepository)
{
    public record Query(Guid UserId, Guid ProfileId, DriveFileImportStatus? StatusFilter = null);

    public record FileImportDto(
        Guid Id,
        string DriveFileId,
        string FileName,
        string MimeType,
        DateTimeOffset FileModifiedTime,
        string Status,
        Guid? MatchedMeetingId,
        DateTimeOffset? ParsedAt,
        DateTimeOffset? ImportedAt,
        string? ErrorMessage,
        DateTimeOffset DiscoveredAt,
        string? ParsedResultJson,
        string DuplicateType,
        decimal DuplicateConfidence,
        string? DuplicateMatchTitle);

    public async Task<IReadOnlyList<FileImportDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken)
            ?? throw new InvalidOperationException("No Google Drive connection found.");

        var imports = query.StatusFilter.HasValue
            ? await fileImportRepository.GetByStatusAsync(connection.Id, query.StatusFilter.Value, cancellationToken)
            : await fileImportRepository.GetByConnectionIdAsync(connection.Id, cancellationToken);

        return imports.Select(f => new FileImportDto(
            f.Id,
            f.DriveFileId,
            f.FileName,
            f.MimeType,
            f.FileModifiedTime,
            f.Status.ToString(),
            f.MatchedMeetingId,
            f.ParsedAt,
            f.ImportedAt,
            f.ErrorMessage,
            f.DiscoveredAt,
            f.ParsedResultJson,
            f.DuplicateType.ToString(),
            f.DuplicateConfidence,
            f.DuplicateMatchTitle)).ToList();
    }
}
