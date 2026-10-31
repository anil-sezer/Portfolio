using System.Threading.Channels;
using Portfolio.Domain.Interfaces.BackgroundServices;

namespace Portfolio.Grpc.BackgroundServices;

public class DatabaseOperationQueue
{
    public const int DefaultCapacity = 1000;

    private readonly Channel<IDatabaseOperationQueueWorker> _channel;
    private readonly int _capacity;

    public DatabaseOperationQueue(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(capacity, 0);

        _capacity = capacity;

        var channelOptions = new BoundedChannelOptions(_capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<IDatabaseOperationQueueWorker>(channelOptions);
    }

    public int Capacity => _capacity;

    public int QueuedCount => _channel.Reader.CanCount ? _channel.Reader.Count : 0;

    public async ValueTask EnqueueAsync(IDatabaseOperationQueueWorker operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await _channel.Writer.WriteAsync(operation, cancellationToken);
    }

    public bool TryEnqueue(IDatabaseOperationQueueWorker operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return _channel.Writer.TryWrite(operation);
    }

    public IAsyncEnumerable<IDatabaseOperationQueueWorker> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public ValueTask<IDatabaseOperationQueueWorker> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }

    public void Complete() => _channel.Writer.Complete();

    public bool TryComplete(Exception? error = null) => _channel.Writer.TryComplete(error);
}
