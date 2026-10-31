using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portfolio.Domain.Interfaces.BackgroundServices;
using Portfolio.Grpc.BackgroundServices;

namespace Portfolio.Test.Grpc;

public class DatabaseOperationQueueWorkerTests
{
    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        return services.BuildServiceProvider();
    }

    private static (DatabaseOperationQueue queue, DatabaseOperationQueueWorker worker) CreateWorker(
        int maxRetryAttempts = 3,
        int initialRetryDelayMs = 20,
        int maxRetryDelayMs = 50,
        int capacity = 100,
        IServiceProvider? serviceProvider = null)
    {
        var queue = new DatabaseOperationQueue(capacity);
        var sp = serviceProvider ?? CreateServiceProvider();
        var worker = new DatabaseOperationQueueWorker(
            queue,
            sp,
            NullLogger<DatabaseOperationQueueWorker>.Instance,
            maxRetryAttempts,
            initialRetryDelayMs,
            maxRetryDelayMs);

        return (queue, worker);
    }

    [Fact]
    public void Constructor_WhenRequiredArgumentsNull_ThrowsArgumentNullException()
    {
        var queue = new DatabaseOperationQueue();
        var sp = CreateServiceProvider();
        var logger = NullLogger<DatabaseOperationQueueWorker>.Instance;

        var act1 = () => new DatabaseOperationQueueWorker(null!, sp, logger);
        var act2 = () => new DatabaseOperationQueueWorker(queue, null!, logger);
        var act3 = () => new DatabaseOperationQueueWorker(queue, sp, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
        act3.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0, 100, 500)]
    [InlineData(3, 0, 500)]
    [InlineData(3, 500, 200)]
    public void Constructor_WhenParametersInvalid_ThrowsArgumentOutOfRangeException(
        int maxRetryAttempts,
        int initialDelayMs,
        int maxDelayMs)
    {
        var queue = new DatabaseOperationQueue();
        var sp = CreateServiceProvider();
        var logger = NullLogger<DatabaseOperationQueueWorker>.Instance;

        var act = () => new DatabaseOperationQueueWorker(queue, sp, logger, maxRetryAttempts, initialDelayMs, maxDelayMs);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ExecuteAsync_ExecutesEnqueuedOperationSuccessfully()
    {
        // Arrange
        var (queue, worker) = CreateWorker();
        var tcs = new TaskCompletionSource<bool>();

        var mockOp = new Mock<IDatabaseOperationQueueWorker>();
        mockOp
            .Setup(x => x.ExecuteAsync(It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                tcs.TrySetResult(true);
                return Task.CompletedTask;
            });

        await queue.EnqueueAsync(mockOp.Object);

        // Act
        await worker.StartAsync(CancellationToken.None);
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(1000));
        await worker.StopAsync(CancellationToken.None);

        // Assert
        completed.Should().Be(tcs.Task);
        mockOp.Verify(x => x.ExecuteAsync(It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationFailsTransiently_RetriesAndSucceeds()
    {
        // Arrange
        var (queue, worker) = CreateWorker();
        var tcs = new TaskCompletionSource<bool>();
        var attemptCount = 0;

        var mockOp = new Mock<IDatabaseOperationQueueWorker>();
        mockOp
            .Setup(x => x.ExecuteAsync(It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    throw new InvalidOperationException("Simulated transient DB error");
                }
                tcs.TrySetResult(true);
                return Task.CompletedTask;
            });

        await queue.EnqueueAsync(mockOp.Object);

        // Act
        await worker.StartAsync(CancellationToken.None);
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        await worker.StopAsync(CancellationToken.None);

        // Assert
        completed.Should().Be(tcs.Task);
        attemptCount.Should().Be(2);
        mockOp.Verify(x => x.ExecuteAsync(It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationPermanentlyFails_ExhaustsRetriesAndContinuesProcessingNextItem()
    {
        // Arrange
        var (queue, worker) = CreateWorker(maxRetryAttempts: 3, initialRetryDelayMs: 10, maxRetryDelayMs: 20);

        var failingAttempts = 0;
        var mockFailingOp = new Mock<IDatabaseOperationQueueWorker>();
        mockFailingOp
            .Setup(x => x.ExecuteAsync(It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                failingAttempts++;
                throw new InvalidOperationException("Permanent DB error");
            });

        var tcsSuccess = new TaskCompletionSource<bool>();
        var mockSucceedingOp = new Mock<IDatabaseOperationQueueWorker>();
        mockSucceedingOp
            .Setup(x => x.ExecuteAsync(It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                tcsSuccess.TrySetResult(true);
                return Task.CompletedTask;
            });

        await queue.EnqueueAsync(mockFailingOp.Object);
        await queue.EnqueueAsync(mockSucceedingOp.Object);

        // Act
        await worker.StartAsync(CancellationToken.None);
        var completed = await Task.WhenAny(tcsSuccess.Task, Task.Delay(3000));
        await worker.StopAsync(CancellationToken.None);

        // Assert
        completed.Should().Be(tcsSuccess.Task);
        failingAttempts.Should().Be(3);
        mockFailingOp.Verify(x => x.ExecuteAsync(It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        mockSucceedingOp.Verify(x => x.ExecuteAsync(It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_DrainsAllRemainingQueuedItemsBeforeCompleting()
    {
        // Arrange
        var (queue, worker) = CreateWorker();
        const int itemCount = 5;
        var processedCount = 0;

        for (var i = 0; i < itemCount; i++)
        {
            var mockOp = new Mock<IDatabaseOperationQueueWorker>();
            mockOp
                .Setup(x => x.ExecuteAsync(It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    Interlocked.Increment(ref processedCount);
                    return Task.CompletedTask;
                });
            await queue.EnqueueAsync(mockOp.Object);
        }

        queue.QueuedCount.Should().Be(itemCount);

        // Act
        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        // Assert
        processedCount.Should().Be(itemCount);
        queue.QueuedCount.Should().Be(0);
    }

    [Fact]
    public async Task StopAsync_WhenTimeoutOccursDuringDrain_AbortsCleanly()
    {
        // Arrange
        var (queue, worker) = CreateWorker();
        var startedTcs = new TaskCompletionSource<bool>();

        var mockOp = new Mock<IDatabaseOperationQueueWorker>();
        mockOp
            .Setup(x => x.ExecuteAsync(It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()))
            .Returns(async (IServiceProvider _, CancellationToken ct) =>
            {
                startedTcs.TrySetResult(true);
                // Hang until cancellation requested
                await Task.Delay(Timeout.Infinite, ct);
            });

        await queue.EnqueueAsync(mockOp.Object);

        await worker.StartAsync(CancellationToken.None);
        await startedTcs.Task;

        // Act: Stop with a cancellation token that cancels after 50ms
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var stopTask = worker.StopAsync(cts.Token);
        var completed = await Task.WhenAny(stopTask, Task.Delay(2000));

        // Assert
        completed.Should().Be(stopTask);
    }
}
