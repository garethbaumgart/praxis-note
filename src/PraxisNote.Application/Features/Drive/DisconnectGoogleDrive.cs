using Microsoft.Extensions.Logging;
using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Application.Features.Drive;

public sealed class DisconnectGoogleDrive(
    IDriveConnectionRepository repository,
    IUnitOfWork unitOfWork,
    IHttpClientFactory httpClientFactory,
    ILogger<DisconnectGoogleDrive> logger)
{
    public record Command(Guid UserId, Guid ProfileId);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetByUserIdAsync(command.UserId, command.ProfileId, cancellationToken);
        if (connection is null)
            return;

        // Best-effort token revocation with Google: prefer refresh token to invalidate the long-lived grant
        var tokenToRevoke = !string.IsNullOrWhiteSpace(connection.RefreshToken)
            ? connection.RefreshToken
            : connection.AccessToken;
        await TryRevokeTokenAsync(tokenToRevoke, cancellationToken);

        repository.Remove(connection);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task TryRevokeTokenAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = token
            });
            var response = await httpClient.PostAsync(
                "https://oauth2.googleapis.com/revoke",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to revoke Google Drive token. Status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error revoking Google Drive token. Continuing with local disconnect.");
        }
    }
}
