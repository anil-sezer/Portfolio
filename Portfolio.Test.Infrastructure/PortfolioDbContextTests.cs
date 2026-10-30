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

    [Fact]
    public async Task PortfolioDbContext_CanPersistEntitiesWithOverlongStrings_AndRetrieveTruncated()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var log = new RequestLog
        {
            UserAgent = new string('A', 5000),
            AcceptLanguage = new string('B', 1000),
            Platform = new string('C', 500),
            Webdriver = false,
            DeviceMemory = new string('D', 100),
            HardwareConcurrency = new string('E', 100),
            MaxTouchPoints = 0,
            DoNotTrack = "1",
            Connection = "ethernet",
            CookieEnabled = true,
            OnLine = true,
            Referrer = "https://example.com/" + new string('F', 3000),
            Resolution = "1920x1080",
            ClientIp = "192.168.1.50",
            Country = "Germany",
            City = "Berlin",
            RequestedUrl = "https://anil-sezer.com",
            Extras = new string('G', 8000)
        };

        var notification = new NotificationToAdmin
        {
            Name = new string('N', 500),
            EmailAddress = "test@example.com",
            Subject = new string('S', 500),
            Message = new string('M', 10000),
            IsItSentSuccessfully = true
        };

        var dailyImage = new DailyImage
        {
            ImageUrl = "https://example.com/image.jpg",
            AltText = new string('T', 3000),
            Source = ImageOfTheDaySource.Bing,
            UrlWorks = true,
            DoIPreferToDisplayThis = true
        };

        // Act
        dbContext.RequestLogs.Add(log);
        dbContext.NotificationsToAdmin.Add(notification);
        dbContext.DailyImages.Add(dailyImage);
        await dbContext.SaveChangesAsync();

        var retrievedLog = await dbContext.RequestLogs.FirstAsync(x => x.ClientIp == "192.168.1.50");
        var retrievedNotification = await dbContext.NotificationsToAdmin.FirstAsync(x => x.EmailAddress == "test@example.com");
        var retrievedImage = await dbContext.DailyImages.FirstAsync(x => x.ImageUrl == "https://example.com/image.jpg");

        // Assert
        retrievedLog.UserAgent.Length.Should().Be(RequestLog.MaxUserAgentLength);
        retrievedLog.Extras.Length.Should().Be(RequestLog.MaxExtrasLength);
        retrievedNotification.Name.Length.Should().Be(NotificationToAdmin.MaxNameLength);
        retrievedNotification.Message.Length.Should().Be(NotificationToAdmin.MaxMessageLength);
        retrievedImage.AltText.Length.Should().Be(DailyImage.MaxAltTextLength);
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

    [Fact]
    public void PortfolioDbContext_Model_ShouldConfigureStringMaxLengthConstraints()
    {
        // Arrange
        using var dbContext = TestDbContextFactory.Create();
        var model = dbContext.Model;

        // DailyImage constraints
        var dailyImage = model.FindEntityType(typeof(DailyImage))!;
        dailyImage.FindProperty(nameof(DailyImage.ImageUrl))!.GetMaxLength().Should().Be(2048);
        dailyImage.FindProperty(nameof(DailyImage.ImageUrl))!.IsNullable.Should().BeFalse();
        dailyImage.FindProperty(nameof(DailyImage.AltText))!.GetMaxLength().Should().Be(1000);
        dailyImage.FindProperty(nameof(DailyImage.AltText))!.IsNullable.Should().BeFalse();

        // NotificationToAdmin constraints
        var notification = model.FindEntityType(typeof(NotificationToAdmin))!;
        notification.FindProperty(nameof(NotificationToAdmin.Name))!.GetMaxLength().Should().Be(100);
        notification.FindProperty(nameof(NotificationToAdmin.Name))!.IsNullable.Should().BeFalse();
        notification.FindProperty(nameof(NotificationToAdmin.EmailAddress))!.GetMaxLength().Should().Be(256);
        notification.FindProperty(nameof(NotificationToAdmin.EmailAddress))!.IsNullable.Should().BeFalse();
        notification.FindProperty(nameof(NotificationToAdmin.Subject))!.GetMaxLength().Should().Be(200);
        notification.FindProperty(nameof(NotificationToAdmin.Subject))!.IsNullable.Should().BeFalse();
        notification.FindProperty(nameof(NotificationToAdmin.Message))!.GetMaxLength().Should().Be(4000);
        notification.FindProperty(nameof(NotificationToAdmin.Message))!.IsNullable.Should().BeFalse();

        // RequestLog constraints
        var requestLog = model.FindEntityType(typeof(RequestLog))!;
        requestLog.FindProperty(nameof(RequestLog.UserAgent))!.GetMaxLength().Should().Be(1000);
        requestLog.FindProperty(nameof(RequestLog.AcceptLanguage))!.GetMaxLength().Should().Be(255);
        requestLog.FindProperty(nameof(RequestLog.Platform))!.GetMaxLength().Should().Be(100);
        requestLog.FindProperty(nameof(RequestLog.DeviceMemory))!.GetMaxLength().Should().Be(32);
        requestLog.FindProperty(nameof(RequestLog.HardwareConcurrency))!.GetMaxLength().Should().Be(32);
        requestLog.FindProperty(nameof(RequestLog.DoNotTrack))!.GetMaxLength().Should().Be(32);
        requestLog.FindProperty(nameof(RequestLog.Connection))!.GetMaxLength().Should().Be(64);
        requestLog.FindProperty(nameof(RequestLog.Referrer))!.GetMaxLength().Should().Be(2048);
        requestLog.FindProperty(nameof(RequestLog.Resolution))!.GetMaxLength().Should().Be(64);
        requestLog.FindProperty(nameof(RequestLog.ClientIp))!.GetMaxLength().Should().Be(64);
        requestLog.FindProperty(nameof(RequestLog.Country))!.GetMaxLength().Should().Be(100);
        requestLog.FindProperty(nameof(RequestLog.City))!.GetMaxLength().Should().Be(100);
        requestLog.FindProperty(nameof(RequestLog.RequestedUrl))!.GetMaxLength().Should().Be(2048);
        requestLog.FindProperty(nameof(RequestLog.Extras))!.GetMaxLength().Should().Be(4000);

        // Required non-nullable checks
        requestLog.FindProperty(nameof(RequestLog.UserAgent))!.IsNullable.Should().BeFalse();
        requestLog.FindProperty(nameof(RequestLog.ClientIp))!.IsNullable.Should().BeFalse();
        requestLog.FindProperty(nameof(RequestLog.Country))!.IsNullable.Should().BeFalse();
        requestLog.FindProperty(nameof(RequestLog.City))!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void PortfolioDbContext_Model_ShouldConfigureTableComments()
    {
        // Arrange
        using var dbContext = TestDbContextFactory.Create();
        var model = dbContext.Model;

        var dailyImage = model.FindEntityType(typeof(DailyImage))!;
        dailyImage.FindAnnotation("Relational:Comment")?.Value.Should().Be("List of daily background images by Bing, NASA, etc. They are great");

        var notification = model.FindEntityType(typeof(NotificationToAdmin))!;
        notification.FindAnnotation("Relational:Comment")?.Value.Should().Be("Notifications that have been sent to the admin via the website ui");

        var requestLog = model.FindEntityType(typeof(RequestLog))!;
        requestLog.FindAnnotation("Relational:Comment")?.Value.Should().Be("Accesses to the website");
    }
}
