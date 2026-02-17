using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.JiraConnections;

namespace PraxisNote.Application.Features.Jira;

public sealed class ConnectJira(IJiraConnectionRepository repository, IUnitOfWork unitOfWork)
{
    public record Command(
        Guid UserId,
        Guid ProfileId,
        string CloudId,
        string SiteUrl,
        string AccessToken,
        string RefreshToken,
        DateTimeOffset TokenExpiresAt);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // Remove existing connection if any (reconnect scenario)
        var existing = await repository.GetByUserIdAndProfileAsync(command.UserId, command.ProfileId, cancellationToken);
        if (existing is not null)
        {
            repository.Remove(existing);
        }

        var connection = JiraConnection.Create(
            command.UserId,
            command.ProfileId,
            command.CloudId,
            command.SiteUrl,
            command.AccessToken,
            command.RefreshToken,
            command.TokenExpiresAt);

        await repository.AddAsync(connection, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
