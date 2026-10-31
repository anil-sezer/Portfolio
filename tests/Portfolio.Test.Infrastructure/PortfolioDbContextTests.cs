using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Test.Shared;

namespace Portfolio.Test.Infrastructure;

public class PortfolioDbContextTests
{
    [Fact]
    public async Task PortfolioDbContext_CanPersistAndRetrieveEntities()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var log = new RequestLog
        {
            UserAgent = "TestAgent",
            AcceptLanguage = "en",
            Platform = "Windows",
            Webdriver = false,
            DeviceMemory = "16",
            HardwareConcurrency = "8",
            MaxTouchPoints = 0,
            DoNotTrack = "1",
            Connection = "ethernet",
            CookieEnabled = true,
            OnLine = true,
            Referrer = "https://google.com",
            Resolution = "1920x1080",
            ClientIp = "192.168.1.50",
            Country = "Germany",
            City = "Berlin",
            RequestedUrl = "https://anil-sezer.com",
            Extras = "{}"
        };

        // Act
        dbContext.RequestLogs.Add(log);
        await dbContext.SaveChangesAsync();

        var retrieved = await dbContext.RequestLogs.FirstOrDefaultAsync(x => x.ClientIp == "192.168.1.50");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.City.Should().Be("Berlin");
        retrieved.Country.Should().Be("Germany");
    }

    [Fact]
    public void PortfolioDbContext_Model_ShouldConfigureIndexesCorrectly()
    {
        // Arrange
        using var dbContext = TestDbContextFactory.Create();
        var model = dbContext.Model;

        // DailyImage index: (UrlWorks, DoIPreferToDisplayThis)
        var dailyImageEntity = model.FindEntityType(typeof(DailyImage));
        dailyImageEntity.Should().NotBeNull();
        dailyImageEntity!.GetIndexes().Should().ContainSingle(idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(DailyImage.UrlWorks), nameof(DailyImage.DoIPreferToDisplayThis) }));

        // NotificationToAdmin index: (CreatedAt, EmailAddress)
        var notificationEntity = model.FindEntityType(typeof(NotificationToAdmin));
        notificationEntity.Should().NotBeNull();
        notificationEntity!.GetIndexes().Should().ContainSingle(idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(NotificationToAdmin.CreatedAt), nameof(NotificationToAdmin.EmailAddress) }));

        // RequestLog index: (ClientIp, City, Country)
        var requestLogEntity = model.FindEntityType(typeof(RequestLog));
        requestLogEntity.Should().NotBeNull();
        requestLogEntity!.GetIndexes().Should().ContainSingle(idx =>
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RequestLog.ClientIp), nameof(RequestLog.City), nameof(RequestLog.Country) }));
    }
}
