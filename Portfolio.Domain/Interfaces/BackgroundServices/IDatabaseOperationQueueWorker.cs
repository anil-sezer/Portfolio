namespace Portfolio.Domain.Interfaces.BackgroundServices;

public interface IDatabaseOperationQueueWorker
{
    Task ExecuteAsync(IServiceProvider serviceProvider);
}
