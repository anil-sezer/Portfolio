using Portfolio.Ui.Services;

namespace Portfolio.Ui.BackgroundServices;

public class HeroImageWarmupService(IServiceProvider serviceProvider, ILogger<HeroImageWarmupService> logger) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Warm up immediately upon startup
        await RefreshHeroImageAsync(stoppingToken);

        // 2. Refresh periodically every 1 hour
        try
        {
            using var timer = new PeriodicTimer(RefreshInterval);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
                await RefreshHeroImageAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown cancellation, exit cleanly
        }
    }

    private async Task RefreshHeroImageAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var backgroundImageService = scope.ServiceProvider.GetRequiredService<BackgroundImageService>();
            await backgroundImageService.RefreshCacheAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown cancellation, exit cleanly
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to refresh hero background image in background: {Message}", ex.Message);
        }
    }
}
