using PraxisNote.Application.Features.Drive;
using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Web.Services;

/// <summary>
/// Background service that periodically polls connected Drive folders for new files,
/// parses them, and either auto-imports or queues for review.
/// </summary>
public sealed class DriveSyncBackgroundJob(
    IServiceScopeFactory scopeFactory,
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
                await orchestrator.SyncConnectionAsync(connection.Id, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to sync Drive connection {ConnectionId} for user {UserId}",
                    connection.Id, connection.UserId);
            }
        }
    }
}
