using Portfolio.Ui.Services;

namespace Portfolio.Ui.BackgroundServices;

public class ClusterStatsWarmupService : BackgroundService
{
    internal static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan DefaultMaxJitter = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan DefaultMaxStartupJitter = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClusterStatsWarmupService> _logger;
    private readonly TimeSpan _baseRefreshInterval;
    private readonly TimeSpan _maxJitter;
    private readonly TimeSpan _maxStartupJitter;
    private readonly Func<double> _nextRandomDouble;

    public ClusterStatsWarmupService(
        IServiceProvider serviceProvider,
        ILogger<ClusterStatsWarmupService> logger)
        : this(
            serviceProvider,
            logger,
            DefaultRefreshInterval,
            DefaultMaxJitter,
            DefaultMaxStartupJitter)
    {
    }

    internal ClusterStatsWarmupService(
        IServiceProvider serviceProvider,
        ILogger<ClusterStatsWarmupService> logger,
        TimeSpan baseRefreshInterval,
        TimeSpan maxJitter,
        TimeSpan maxStartupJitter,
        Func<double>? nextRandomDouble = null)
    {
        if (baseRefreshInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(baseRefreshInterval), "Base refresh interval must be positive.");
        if (maxJitter < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxJitter), "Max jitter cannot be negative.");
        if (maxStartupJitter < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxStartupJitter), "Max startup jitter cannot be negative.");

        _serviceProvider = serviceProvider;
        _logger = logger;
        _baseRefreshInterval = baseRefreshInterval;
        _maxJitter = maxJitter;
        _maxStartupJitter = maxStartupJitter;
        _nextRandomDouble = nextRandomDouble ?? (() => Random.Shared.NextDouble());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // 1. Stagger initial warmup across replicas, then warm up cache
            var startupDelay = GetStartupDelay();
            if (startupDelay > TimeSpan.Zero)
            {
                await Task.Delay(startupDelay, stoppingToken);
            }

            await RefreshStatsAsync(stoppingToken);

            // 2. Refresh periodically with random jitter so replicas do not poll kube-apiserver concurrently
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = GetNextRefreshDelay();
                await Task.Delay(delay, stoppingToken);
                await RefreshStatsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown cancellation, exit cleanly
        }
    }

    internal TimeSpan GetStartupDelay() => ComputeJitter(_maxStartupJitter);

    internal TimeSpan GetNextRefreshDelay() => _baseRefreshInterval + ComputeJitter(_maxJitter);

    private TimeSpan ComputeJitter(TimeSpan maxJitter)
    {
        if (maxJitter <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var fraction = Math.Clamp(_nextRandomDouble(), 0.0, 1.0);
        return TimeSpan.FromMilliseconds(fraction * maxJitter.TotalMilliseconds);
    }

    private async Task RefreshStatsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var clusterStatsService = scope.ServiceProvider.GetRequiredService<ClusterStatsService>();
            await clusterStatsService.RefreshCacheAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown cancellation, exit cleanly
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh cluster stats in background: {Message}", ex.Message);
        }
    }
}
