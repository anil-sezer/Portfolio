using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Grpc;
using Portfolio.Grpc.BackgroundServices;
using Portfolio.Infrastructure;

namespace Portfolio.Test.Grpc;

public class LogVisitorInfoQueuedOperationTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesAndPersistsRequestLogWithMappedFields()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddDbContext<PortfolioDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));

        var serviceProvider = services.BuildServiceProvider();

        var request = new StoreVisitorInfoRequest
        {
            IpAddress = "198.51.100.42",
            RequestedUrl = "https://anilsezer.net/resume",
            Language = "en-US",
            Platform = "macOS",
            Referrer = "https://linkedin.com",
            UserAgent = "Mozilla/5.0 (Macintosh)",
            DoNotTrack = "1",
            Connection = "wifi",
            Resolution = "2560x1600",
            DeviceMemory = "16",
            OnLine = true,
            HardwareConcurrency = "8",
            Webdriver = false,
            CookieEnabled = true,
            MaxTouchPoints = 0,
            Extras = "{\"host\":\"anilsezer.net\"}"
        };

        var operation = new LogVisitorInfoQueuedOperation
        {
            Request = request
        };

        // Act
        await operation.ExecuteAsync(serviceProvider);

        // Assert
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();

        var savedLog = await db.RequestLogs.FirstOrDefaultAsync(x => x.ClientIp == "198.51.100.42");
        savedLog.Should().NotBeNull();
        savedLog!.RequestedUrl.Should().Be("https://anilsezer.net/resume");
        savedLog.AcceptLanguage.Should().Be("en-US");
        savedLog.Platform.Should().Be("macOS");
        savedLog.Referrer.Should().Be("https://linkedin.com");
        savedLog.UserAgent.Should().Be("Mozilla/5.0 (Macintosh)");
        savedLog.DoNotTrack.Should().Be("1");
        savedLog.Connection.Should().Be("wifi");
        savedLog.Resolution.Should().Be("2560x1600");
        savedLog.DeviceMemory.Should().Be("16");
        savedLog.OnLine.Should().BeTrue();
        savedLog.HardwareConcurrency.Should().Be("8");
        savedLog.Webdriver.Should().BeFalse();
        savedLog.CookieEnabled.Should().BeTrue();
        savedLog.MaxTouchPoints.Should().Be(0);
        savedLog.Extras.Should().Be("{\"host\":\"anilsezer.net\"}");
        savedLog.City.Should().BeEmpty();
        savedLog.Country.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<PortfolioDbContext>(opt =>
            opt.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        var serviceProvider = services.BuildServiceProvider();

        var operation = new LogVisitorInfoQueuedOperation
        {
            Request = new StoreVisitorInfoRequest
            {
                IpAddress = "127.0.0.1",
                RequestedUrl = "https://anilsezer.net"
            }
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await operation.ExecuteAsync(serviceProvider, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
