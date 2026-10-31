using Portfolio.Domain.Interfaces.BackgroundServices;

namespace Portfolio.Grpc.BackgroundServices;

public class DatabaseOperationQueueWorker : BackgroundService
{
    public const int DefaultMaxRetryAttempts = 3;
    public const int DefaultInitialRetryDelayMs = 500;
    public const int DefaultMaxRetryDelayMs = 3000;

    private readonly DatabaseOperationQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseOperationQueueWorker> _logger;
    private readonly int _maxRetryAttempts;
    private readonly int _initialRetryDelayMs;
    private readonly int _maxRetryDelayMs;
    private readonly CancellationTokenSource _drainCts = new();

    public DatabaseOperationQueueWorker(
        DatabaseOperationQueue queue,
        IServiceProvider serviceProvider,
        ILogger<DatabaseOperationQueueWorker> logger,
        int maxRetryAttempts = DefaultMaxRetryAttempts,
        int initialRetryDelayMs = DefaultInitialRetryDelayMs,
        int maxRetryDelayMs = DefaultMaxRetryDelayMs)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        ArgumentOutOfRangeException.ThrowIfLessThan(maxRetryAttempts, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialRetryDelayMs, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRetryDelayMs, initialRetryDelayMs);

        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _maxRetryAttempts = maxRetryAttempts;
        _initialRetryDelayMs = initialRetryDelayMs;
        _maxRetryDelayMs = maxRetryDelayMs;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DatabaseOperationQueueWorker started with bounded capacity {Capacity}.", _queue.Capacity);

        try
        {
            await foreach (var operation in _queue.ReadAllAsync(_drainCts.Token))
            {
                await ProcessOperationWithRetryAsync(operation, _drainCts.Token);
            }
        }
        catch (OperationCanceledException) when (_drainCts.IsCancellationRequested)
        {
            _logger.LogWarning("DatabaseOperationQueueWorker reader loop terminated due to shutdown timeout.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error in DatabaseOperationQueueWorker execution loop.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DatabaseOperationQueueWorker is stopping. Marking queue complete and draining {Count} operations...",
            _queue.QueuedCount);

        // Mark queue writer complete so ReadAllAsync exits once empty
        _queue.Complete();

        if (ExecuteTask != null)
        {
            using var reg = cancellationToken.Register(() =>
            {
                _logger.LogWarning("Host shutdown timeout reached while draining DatabaseOperationQueue. Forcing cancellation.");
                _drainCts.Cancel();
            });

            try
            {
                await ExecuteTask;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Draining aborted due to host shutdown timeout. Unprocessed count: {Count}", _queue.QueuedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception encountered while waiting for DatabaseOperationQueueWorker to drain.");
            }
        }

        _logger.LogInformation("DatabaseOperationQueueWorker stopped.");
    }

    private async Task ProcessOperationWithRetryAsync(IDatabaseOperationQueueWorker operation, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _maxRetryAttempts; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Database operation dropped due to cancellation before attempt {Attempt}/{MaxAttempts}.", attempt, _maxRetryAttempts);
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                await operation.ExecuteAsync(scope.ServiceProvider, cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Database operation canceled during execution.");
                return;
            }
            catch (Exception ex)
            {
                if (attempt >= _maxRetryAttempts)
                {
                    _logger.LogError(
                        ex,
                        "Failed to execute background database operation after {MaxAttempts} attempts. Operation: {OperationType}",
                        _maxRetryAttempts,
                        operation.GetType().Name);
                    return;
                }

                var factor = Math.Pow(2, attempt - 1);
                var computedDelayMs = (int)Math.Min(_maxRetryDelayMs, _initialRetryDelayMs * factor);
                var jitterMs = Random.Shared.Next(0, Math.Min(100, computedDelayMs / 2 + 1));
                var delay = TimeSpan.FromMilliseconds(computedDelayMs + jitterMs);

                _logger.LogWarning(
                    ex,
                    "Database operation {OperationType} failed on attempt {Attempt}/{MaxAttempts}. Retrying in {DelayMs:F0}ms...",
                    operation.GetType().Name,
                    attempt,
                    _maxRetryAttempts,
                    delay.TotalMilliseconds);

                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Retry delay canceled due to shutdown.");
                    return;
                }
            }
        }
    }

    public override void Dispose()
    {
        _drainCts.Cancel();
        _drainCts.Dispose();
        base.Dispose();
    }
}
