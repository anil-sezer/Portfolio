using System.Threading.Channels;
using Portfolio.Domain.Interfaces.BackgroundServices;

namespace Portfolio.Grpc.BackgroundServices;

public class DatabaseOperationQueueWorker(IServiceProvider serviceProvider) : BackgroundService
{
    private static readonly Channel<IDatabaseOperationQueueWorker> Channel = System.Threading.Channels.Channel.CreateUnbounded<IDatabaseOperationQueueWorker>();

    public static async Task EnqueueAsync<T>(T operation, CancellationToken cancellationToken = default) where T : IDatabaseOperationQueueWorker
    {
        await Channel.Writer.WriteAsync(operation, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var operation in Channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                await operation.ExecuteAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                // Log error but continue processing
                Log.Error(ex, "Failed to execute background database operation");
            }
        }
    }
}