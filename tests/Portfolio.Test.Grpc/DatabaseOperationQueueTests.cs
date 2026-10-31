using System.Threading.Channels;
using FluentAssertions;
using Moq;
using Portfolio.Domain.Interfaces.BackgroundServices;
using Portfolio.Grpc.BackgroundServices;

namespace Portfolio.Test.Grpc;

public class DatabaseOperationQueueTests
{
    private static DatabaseOperationQueue CreateQueue(int capacity = DatabaseOperationQueue.DefaultCapacity)
    {
        return new DatabaseOperationQueue(capacity);
    }

    private static IDatabaseOperationQueueWorker CreateMockOperation()
    {
        return new Mock<IDatabaseOperationQueueWorker>().Object;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenCapacityIsZeroOrNegative_ThrowsArgumentOutOfRangeException(int capacity)
    {
        // Arrange & Act
        var act = () => new DatabaseOperationQueue(capacity);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task EnqueueAsync_And_DequeueAsync_OperatesInFifoOrder()
    {
        // Arrange
        var queue = CreateQueue(10);
        var op1 = CreateMockOperation();
        var op2 = CreateMockOperation();

        // Act
        await queue.EnqueueAsync(op1);
        await queue.EnqueueAsync(op2);

        // Assert
        queue.QueuedCount.Should().Be(2);

        var dequeued1 = await queue.DequeueAsync();
        var dequeued2 = await queue.DequeueAsync();

        dequeued1.Should().BeSameAs(op1);
        dequeued2.Should().BeSameAs(op2);
        queue.QueuedCount.Should().Be(0);
    }

    [Fact]
    public async Task TryEnqueue_WhenQueueIsFull_ReturnsFalse()
    {
        // Arrange
        var queue = CreateQueue(2);
        var op1 = CreateMockOperation();
        var op2 = CreateMockOperation();
        var op3 = CreateMockOperation();

        // Act
        var result1 = queue.TryEnqueue(op1);
        var result2 = queue.TryEnqueue(op2);
        var result3 = queue.TryEnqueue(op3);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeFalse();
        queue.QueuedCount.Should().Be(2);

        // Dequeue one and verify we can enqueue again
        await queue.DequeueAsync();
        var result4 = queue.TryEnqueue(op3);
        result4.Should().BeTrue();
    }

    [Fact]
    public async Task EnqueueAsync_WhenQueueIsFull_WaitsUntilSpaceFreed()
    {
        // Arrange
        var queue = CreateQueue(1);
        var op1 = CreateMockOperation();
        var op2 = CreateMockOperation();

        await queue.EnqueueAsync(op1);

        // Act: Enqueue 2nd item into full queue - should not complete immediately
        var enqueueTask = queue.EnqueueAsync(op2).AsTask();
        var completed = await Task.WhenAny(enqueueTask, Task.Delay(50));
        completed.Should().NotBe(enqueueTask); // Still waiting

        // Dequeue the first item to free up space
        var dequeued = await queue.DequeueAsync();
        dequeued.Should().BeSameAs(op1);

        // Now the second enqueue task should complete
        await enqueueTask;
        queue.QueuedCount.Should().Be(1);

        var dequeued2 = await queue.DequeueAsync();
        dequeued2.Should().BeSameAs(op2);
    }

    [Fact]
    public async Task EnqueueAsync_WhenCancelledWhileWaiting_ThrowsOperationCanceledException()
    {
        // Arrange
        var queue = CreateQueue(1);
        var op1 = CreateMockOperation();
        var op2 = CreateMockOperation();

        await queue.EnqueueAsync(op1);

        using var cts = new CancellationTokenSource();

        // Act
        var enqueueTask = queue.EnqueueAsync(op2, cts.Token).AsTask();
        cts.Cancel();

        // Assert
        var act = async () => await enqueueTask;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Complete_PreventsFurtherEnqueuesAndCausesChannelClosedException()
    {
        // Arrange
        var queue = CreateQueue(10);
        var op1 = CreateMockOperation();
        await queue.EnqueueAsync(op1);

        // Act
        queue.Complete();

        // Assert
        var act = async () => await queue.EnqueueAsync(CreateMockOperation());
        await act.Should().ThrowAsync<ChannelClosedException>();

        // Existing items can still be dequeued
        var dequeued = await queue.DequeueAsync();
        dequeued.Should().BeSameAs(op1);
    }

    [Fact]
    public async Task EnqueueAsync_WhenOperationIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var queue = CreateQueue(10);

        // Act & Assert
        var act = async () => await queue.EnqueueAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
