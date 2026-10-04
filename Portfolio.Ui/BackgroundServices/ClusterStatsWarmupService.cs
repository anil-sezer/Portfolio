using Portfolio.Ui.Services;

namespace Portfolio.Ui.BackgroundServices;

public class ClusterStatsWarmupService(IServiceProvider serviceProvider) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Warm up immediately upon startup
        await RefreshStatsAsync(stoppingToken);

        // 2. Refresh periodically every 30 seconds
        using var timer = new PeriodicTimer(RefreshInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshStatsAsync(stoppingToken);
        }
    }

    private async Task RefreshStatsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var clusterStatsService = scope.ServiceProvider.GetRequiredService<ClusterStatsService>();
            await clusterStatsService.RefreshCacheAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown cancellation, exit cleanly
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to refresh cluster stats in background: {Message}", ex.Message);
        }
    }
}
