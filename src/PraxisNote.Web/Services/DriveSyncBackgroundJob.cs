using PraxisNote.Application.Features.Drive;
using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Web.Services;

/// <summary>
/// Background service that periodically polls connected Drive folders for new files,
/// parses them, and either auto-imports or queues for review.
/// </summary>
public sealed class DriveSyncBackgroundJob(
    IServiceScopeFactory scopeFactory,
    NotificationSseManager sseManager,
    ILogger<DriveSyncBackgroundJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Drive sync background job started");

        using var timer = new PeriodicTimer(PollingInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessDueConnectionsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled error in Drive sync job");
            }
        }
    }

    private async Task ProcessDueConnectionsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var connRepo = scope.ServiceProvider.GetRequiredService<IDriveConnectionRepository>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<DriveSyncOrchestrator>();

        var dueConnections = await connRepo.GetConnectionsDueForSyncAsync(ct);

        if (dueConnections.Count > 0)
        {
            logger.LogDebug("Found {Count} Drive connections due for sync", dueConnections.Count);
        }

        // Process sequentially to avoid DbContext threading issues
        foreach (var connection in dueConnections)
        {
            try
            {
                var result = await orchestrator.SyncConnectionAsync(connection.Id, ct);

                // Send SSE notifications (best-effort; failures should not affect sync)
                await SendSyncNotificationsAsync(connection.UserId, result);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to sync Drive connection {ConnectionId} for user {UserId}",
                    connection.Id, connection.UserId);
            }
        }
    }

    private async Task SendSyncNotificationsAsync(Guid userId, DriveSyncOrchestrator.SyncResult result)
    {
        try
        {
            if (result.FilesPendingReview > 0)
            {
                await sseManager.BroadcastToUserAsync(userId, "drive-sync", new
                {
                    type = "pending_review",
                    count = result.FilesPendingReview,
                    message = $"{result.FilesPendingReview} new file{(result.FilesPendingReview == 1 ? "" : "s")} ready for review"
                });
            }

            if (result.FilesImported > 0)
            {
                await sseManager.BroadcastToUserAsync(userId, "drive-sync", new
                {
                    type = "auto_imported",
                    count = result.FilesImported,
                    message = $"{result.FilesImported} meeting{(result.FilesImported == 1 ? "" : "s")} auto-imported from Drive"
                });
            }

            if (result.Error is not null)
            {
                await sseManager.BroadcastToUserAsync(userId, "drive-sync", new
                {
                    type = "error",
                    message = result.Error
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send SSE notification for Drive sync to user {UserId}", userId);
        }
    }
}
