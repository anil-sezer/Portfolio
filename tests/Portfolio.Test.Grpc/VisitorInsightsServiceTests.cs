using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces.BackgroundServices;
using Portfolio.Grpc;
using Portfolio.Grpc.BackgroundServices;
using Portfolio.Grpc.Services.VisitorInsightsServices;
using Portfolio.Infrastructure;
using Portfolio.Test.Shared;

namespace Portfolio.Test.Grpc;

public class VisitorInsightsServiceTests
{
    private static RequestLog CreateRequestLog(string ip, string city, string country) => new()
    {
        ClientIp = ip,
        City = city,
        Country = country,
        UserAgent = "TestUA",
        AcceptLanguage = "en",
        Platform = "Linux",
        Webdriver = false,
        DeviceMemory = "8",
        HardwareConcurrency = "4",
        MaxTouchPoints = 0,
        DoNotTrack = "0",
        Connection = "wifi",
        CookieEnabled = true,
        OnLine = true,
        Referrer = "",
        Resolution = "1920x1080",
        RequestedUrl = "https://example.com",
        Extras = "{}"
    };

    private static VisitorInsightsService CreateService(PortfolioDbContext dbContext, DatabaseOperationQueue? queue = null)
    {
        return new VisitorInsightsService(
            dbContext,
            queue ?? new DatabaseOperationQueue(),
            NullLogger<VisitorInsightsService>.Instance);
    }

    [Fact]
    public async Task GetIpsToCheck_ReturnsOnlyRowsWithNonEmptyIpAndEmptyCityAndCountry()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);
        var context = TestServerCallContext.Create();

        // Matching: IP present, city and country empty
        var matchingLog = CreateRequestLog("203.0.113.1", "", "");
        // Non-matching: Empty IP
        var emptyIpLog = CreateRequestLog("", "", "");
        // Non-matching: City already resolved
        var resolvedCityLog = CreateRequestLog("203.0.113.2", "London", "");
        // Non-matching: Country already resolved
        var resolvedCountryLog = CreateRequestLog("203.0.113.3", "", "UK");
        // Non-matching: Both city and country resolved
        var fullyResolvedLog = CreateRequestLog("203.0.113.4", "Paris", "France");

        dbContext.RequestLogs.AddRange(matchingLog, emptyIpLog, resolvedCityLog, resolvedCountryLog, fullyResolvedLog);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await service.GetIpsToCheck(new Empty(), context);

        // Assert
        response.Should().NotBeNull();
        response.Ips.Should().HaveCount(1);

        var item = response.Ips[0];
        item.EntityId.Should().Be(matchingLog.Id);
        item.IpAddress.Should().Be("203.0.113.1");
        item.City.Should().BeEmpty();
        item.Country.Should().BeEmpty();
        item.Operation.Should().Be(DbOperationForThisRow.Unprocessed);
    }

    [Fact]
    public async Task GetIpsToCheck_WhenNoMatchingRows_ReturnsEmptyList()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);
        var context = TestServerCallContext.Create();

        // Act
        var response = await service.GetIpsToCheck(new Empty(), context);

        // Assert
        response.Should().NotBeNull();
        response.Ips.Should().BeEmpty();
    }

    [Fact]
    public async Task GetIpsToCheck_WithMultipleMatchingRows_ReturnsAllMatchingRows()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);
        var context = TestServerCallContext.Create();

        var log1 = CreateRequestLog("1.1.1.1", "", "");
        var log2 = CreateRequestLog("2.2.2.2", "", "");
        var log3 = CreateRequestLog("3.3.3.3", "", "");

        dbContext.RequestLogs.AddRange(log1, log2, log3);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await service.GetIpsToCheck(new Empty(), context);

        // Assert
        response.Should().NotBeNull();
        response.Ips.Should().HaveCount(3);
        response.Ips.Select(x => x.IpAddress).Should().Contain(new[] { "1.1.1.1", "2.2.2.2", "3.3.3.3" });
    }

    [Fact]
    public async Task PersistCheckedIps_WhenUpdateOperation_UpdatesCityAndCountryInDatabase()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);
        var context = TestServerCallContext.Create();

        var log = CreateRequestLog("198.51.100.10", "", "");
        dbContext.RequestLogs.Add(log);
        await dbContext.SaveChangesAsync();

        var request = new PersistCheckedIpsRequest();
        request.CheckedIps.Add(new IpCheckDto
        {
            EntityId = log.Id,
            IpAddress = "198.51.100.10",
            City = "Ankara",
            Country = "Turkey",
            Operation = DbOperationForThisRow.Update
        });

        // Act
        var response = await service.PersistCheckedIps(request, context);

        // Assert
        response.Should().NotBeNull();
        var updated = await dbContext.RequestLogs.FindAsync(log.Id);
        updated.Should().NotBeNull();
        updated!.City.Should().Be("Ankara");
        updated.Country.Should().Be("Turkey");
    }

    [Fact]
    public async Task PersistCheckedIps_WhenNonExistentEntityId_CompletesWithoutError()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);
        var context = TestServerCallContext.Create();

        var request = new PersistCheckedIpsRequest();
        request.CheckedIps.Add(new IpCheckDto
        {
            EntityId = 999999,
            IpAddress = "10.0.0.1",
            City = "Unknown",
            Country = "Unknown",
            Operation = DbOperationForThisRow.Update
        });

        // Act
        var act = async () => await service.PersistCheckedIps(request, context);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetIpsToCheck_LimitsResultsToMaxBatchSize()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);
        var context = TestServerCallContext.Create();

        var logs = Enumerable.Range(1, VisitorInsightsService.MaxBatchSize + 15)
            .Select(i => CreateRequestLog($"10.0.0.{i % 250}", "", ""))
            .ToArray();

        dbContext.RequestLogs.AddRange(logs);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await service.GetIpsToCheck(new Empty(), context);

        // Assert
        response.Should().NotBeNull();
        response.Ips.Should().HaveCount(VisitorInsightsService.MaxBatchSize);
    }

    [Fact]
    public async Task StoreVisitorInfo_EnqueuesOperationToDatabaseQueue()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var queue = new DatabaseOperationQueue(capacity: 10);
        var service = CreateService(dbContext, queue);
        var context = TestServerCallContext.Create();

        var request = new StoreVisitorInfoRequest
        {
            IpAddress = "192.0.2.1",
            RequestedUrl = "https://anilsezer.net/about",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        var response = await service.StoreVisitorInfo(request, context);

        // Assert
        response.Should().NotBeNull();
        queue.QueuedCount.Should().Be(1);

        var dequeued = await queue.DequeueAsync();
        dequeued.Should().BeOfType<LogVisitorInfoQueuedOperation>();
        var logOp = (LogVisitorInfoQueuedOperation)dequeued;
        logOp.Request.IpAddress.Should().Be("192.0.2.1");
        logOp.Request.RequestedUrl.Should().Be("https://anilsezer.net/about");
    }

    [Fact]
    public async Task StoreVisitorInfo_WhenChannelClosed_ThrowsRpcExceptionWithUnavailable()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var queue = new DatabaseOperationQueue(capacity: 10);
        queue.Complete(); // Simulate shutdown

        var service = CreateService(dbContext, queue);
        var context = TestServerCallContext.Create();

        var request = new StoreVisitorInfoRequest
        {
            IpAddress = "192.0.2.2",
            RequestedUrl = "https://anilsezer.net"
        };

        // Act
        var act = async () => await service.StoreVisitorInfo(request, context);

        // Assert
        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.Unavailable);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("127.0.0.5")]
    public async Task StoreVisitorInfo_WhenIpIsLoopback_SkipsQueueing(string loopbackIp)
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var queue = new DatabaseOperationQueue(capacity: 10);
        var service = CreateService(dbContext, queue);
        var context = TestServerCallContext.Create();

        var request = new StoreVisitorInfoRequest
        {
            IpAddress = loopbackIp,
            RequestedUrl = "https://anilsezer.net/test"
        };

        // Act
        var response = await service.StoreVisitorInfo(request, context);

        // Assert
        response.Should().NotBeNull();
        queue.QueuedCount.Should().Be(0);
    }
}
