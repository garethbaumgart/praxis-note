using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Application.Features.Drive;

public sealed class ConnectGoogleDrive(IDriveConnectionRepository repository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid ProfileId, string AccessToken, string RefreshToken, DateTimeOffset TokenExpiresAt);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // Remove existing connection if any (reconnect scenario)
        var existing = await repository.GetByUserIdAsync(command.UserId, command.ProfileId, cancellationToken);
        if (existing is not null)
        {
            repository.Remove(existing);
        }

        var connection = DriveConnection.Create(
            command.UserId,
            command.ProfileId,
            "Google",
            command.AccessToken,
            command.RefreshToken,
            command.TokenExpiresAt);

        await repository.AddAsync(connection, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
