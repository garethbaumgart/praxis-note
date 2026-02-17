using PraxisNote.Domain.Aggregates.JiraConnections;

namespace PraxisNote.Application.Features.Jira;

public sealed class GetJiraConnectionStatus(IJiraConnectionRepository repository)
{
    public record Query(Guid UserId, Guid ProfileId);
    public record Result(bool IsConnected, string? SiteUrl, DateTimeOffset? ConnectedAt);

    public async Task<Result> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetByUserIdAndProfileAsync(query.UserId, query.ProfileId, cancellationToken);

        if (connection is null)
            return new Result(false, null, null);

        return new Result(true, connection.SiteUrl, connection.ConnectedAt);
    }
}
