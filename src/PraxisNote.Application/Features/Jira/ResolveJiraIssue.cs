using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Jira.Services;
using PraxisNote.Domain.Aggregates.JiraConnections;

namespace PraxisNote.Application.Features.Jira;

public sealed class ResolveJiraIssue(
    IJiraConnectionRepository repository,
    IJiraService jiraService,
    IUnitOfWork unitOfWork)
{
    public record Query(Guid UserId, Guid ProfileId, string IssueKey);

    public async Task<JiraIssueDto> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetByUserIdAndProfileAsync(query.UserId, query.ProfileId, cancellationToken)
            ?? throw new InvalidOperationException("No Jira connection found. Please connect Jira in Settings.");

        // Refresh token if expired
        if (connection.IsTokenExpired())
        {
            var refreshResult = await jiraService.RefreshAccessTokenAsync(connection.RefreshToken, cancellationToken);
            connection.UpdateTokens(refreshResult.AccessToken, refreshResult.ExpiresAt, refreshResult.RefreshToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await jiraService.GetIssueAsync(connection.CloudId, query.IssueKey, connection.AccessToken, cancellationToken);
    }
}
