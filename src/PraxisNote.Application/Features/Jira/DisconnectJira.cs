using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.JiraConnections;

namespace PraxisNote.Application.Features.Jira;

public sealed class DisconnectJira(IJiraConnectionRepository repository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid ProfileId);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetByUserIdAndProfileAsync(command.UserId, command.ProfileId, cancellationToken);
        if (connection is null)
            return;

        repository.Remove(connection);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
