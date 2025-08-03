using System.Threading.Channels;
using Portfolio.Domain.Interfaces.BackgroundServices;

namespace Portfolio.Grpc.BackgroundServices;

public class DatabaseOperationQueueWorker(IServiceProvider serviceProvider) : BackgroundService
{
    private static readonly Channel<IDatabaseOperationQueueWorker> _channel = Channel.CreateUnbounded<IDatabaseOperationQueueWorker>();

    public static async Task EnqueueAsync<T>(T operation) where T : IDatabaseOperationQueueWorker
    {
        await _channel.Writer.WriteAsync(operation);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var operation in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                await operation.ExecuteAsync(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                // Log error but continue processing
                Log.Error(ex, "Failed to execute background database operation");
            }
        }
    }
}