using PraxisNote.Application.Features.Insights;

namespace PraxisNote.Web.BackgroundServices;

public sealed class ArchetypeSnapshotWorker(
    IServiceProvider serviceProvider,
    ILogger<ArchetypeSnapshotWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Archetype Snapshot Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = GetNextMondayMidnight(now);
                var delay = nextRun - now;

                logger.LogInformation(
                    "Next archetype snapshot generation scheduled for {NextRun} (in {Hours}h {Minutes}m)",
                    nextRun,
                    delay.Hours,
                    delay.Minutes);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                logger.LogInformation("Generating weekly archetype snapshots");

                using var scope = serviceProvider.CreateScope();
                var generator = scope.ServiceProvider.GetRequiredService<GenerateWeeklyArchetypeSnapshots>();
                await generator.ExecuteAsync(stoppingToken);

                logger.LogInformation("Weekly archetype snapshots generated successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating weekly archetype snapshots");
                // Wait 1 hour before retrying on error
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        logger.LogInformation("Archetype Snapshot Worker stopping");
    }

    private static DateTime GetNextMondayMidnight(DateTime now)
    {
        // Calculate next Monday at 00:00 UTC
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0 && now.TimeOfDay > TimeSpan.Zero)
        {
            daysUntilMonday = 7; // If it's Monday but past midnight, wait until next Monday
        }
        return now.Date.AddDays(daysUntilMonday);
    }
}
