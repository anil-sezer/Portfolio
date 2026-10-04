using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
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
    public async Task PortfolioDbContext_CanFilterAndOrderDailyImages()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.DailyImages.AddRange(
            new DailyImage { ImageUrl = "https://example.com/1.jpg", AltText = "1", Source = ImageOfTheDaySource.Bing, UrlWorks = true, DoIPreferToDisplayThis = true },
            new DailyImage { ImageUrl = "https://example.com/2.jpg", AltText = "2", Source = ImageOfTheDaySource.NASA, UrlWorks = false, DoIPreferToDisplayThis = true },
            new DailyImage { ImageUrl = "https://example.com/3.jpg", AltText = "3", Source = ImageOfTheDaySource.Bing, UrlWorks = true, DoIPreferToDisplayThis = false },
            new DailyImage { ImageUrl = "https://example.com/4.jpg", AltText = "4", Source = ImageOfTheDaySource.NASA, UrlWorks = true, DoIPreferToDisplayThis = true }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var activeImages = await dbContext.DailyImages
            .Where(x => x.UrlWorks && x.DoIPreferToDisplayThis)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        // Assert
        activeImages.Should().HaveCount(2);
        activeImages[0].ImageUrl.Should().Be("https://example.com/4.jpg");
        activeImages[1].ImageUrl.Should().Be("https://example.com/1.jpg");
    }
}
