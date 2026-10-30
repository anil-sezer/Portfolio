using FluentAssertions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Test.Domain;

public class EntityTruncationTests
{
    [Fact]
    public void DailyImage_ShouldTruncateAltText_AndExcludeImageUrlFromTruncation()
    {
        var longAltText = new string('A', 1500);
        var longImageUrl = "https://example.com/" + new string('U', 2500);

        var image = new DailyImage
        {
            AltText = longAltText,
            ImageUrl = longImageUrl,
            Source = ImageOfTheDaySource.Bing,
            UrlWorks = true,
            DoIPreferToDisplayThis = true
        };

        image.AltText.Length.Should().Be(DailyImage.MaxAltTextLength);
        image.AltText.Should().Be(new string('A', DailyImage.MaxAltTextLength - 4) + " ...");

        // ImageUrl must NOT be truncated per requirements
        image.ImageUrl.Should().Be(longImageUrl);
        image.ImageUrl.Length.Should().Be(longImageUrl.Length);
    }

    [Fact]
    public void NotificationToAdmin_ShouldTruncateAllStringProperties()
    {
        var notification = new NotificationToAdmin
        {
            Name = new string('N', 200),
            EmailAddress = new string('E', 300) + "@test.com",
            Subject = new string('S', 300),
            Message = new string('M', 5000),
            IsItSentSuccessfully = true
        };

        notification.Name.Length.Should().Be(NotificationToAdmin.MaxNameLength);
        notification.EmailAddress.Length.Should().Be(NotificationToAdmin.MaxEmailAddressLength);
        notification.Subject.Length.Should().Be(NotificationToAdmin.MaxSubjectLength);
        notification.Message.Length.Should().Be(NotificationToAdmin.MaxMessageLength);
    }

    [Fact]
    public void RequestLog_ShouldTruncateAllStringPropertiesOnCreation()
    {
        var log = new RequestLog
        {
            UserAgent = new string('U', 2000),
            AcceptLanguage = new string('L', 500),
            Platform = new string('P', 200),
            Webdriver = false,
            DeviceMemory = new string('D', 100),
            HardwareConcurrency = new string('H', 100),
            MaxTouchPoints = 5,
            DoNotTrack = new string('T', 100),
            Connection = new string('C', 200),
            CookieEnabled = true,
            OnLine = true,
            Referrer = new string('R', 3000),
            Resolution = new string('X', 200),
            ClientIp = new string('I', 200),
            Country = new string('K', 200),
            City = new string('Y', 200),
            RequestedUrl = new string('Q', 3000),
            Extras = new string('E', 6000)
        };

        log.UserAgent.Length.Should().Be(RequestLog.MaxUserAgentLength);
        log.AcceptLanguage.Length.Should().Be(RequestLog.MaxAcceptLanguageLength);
        log.Platform.Length.Should().Be(RequestLog.MaxPlatformLength);
        log.DeviceMemory.Length.Should().Be(RequestLog.MaxDeviceMemoryLength);
        log.HardwareConcurrency.Length.Should().Be(RequestLog.MaxHardwareConcurrencyLength);
        log.DoNotTrack.Length.Should().Be(RequestLog.MaxDoNotTrackLength);
        log.Connection.Length.Should().Be(RequestLog.MaxConnectionLength);
        log.Referrer.Length.Should().Be(RequestLog.MaxReferrerLength);
        log.Resolution.Length.Should().Be(RequestLog.MaxResolutionLength);
        log.ClientIp.Length.Should().Be(RequestLog.MaxClientIpLength);
        log.Country.Length.Should().Be(RequestLog.MaxCountryLength);
        log.City.Length.Should().Be(RequestLog.MaxCityLength);
        log.RequestedUrl.Length.Should().Be(RequestLog.MaxRequestedUrlLength);
        log.Extras.Length.Should().Be(RequestLog.MaxExtrasLength);
    }

    [Fact]
    public void RequestLog_UpdateLocation_ShouldTruncateCityAndCountry()
    {
        var log = new RequestLog
        {
            UserAgent = "UA",
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
            Referrer = "https://example.com",
            Resolution = "1920x1080",
            ClientIp = "1.2.3.4",
            Country = "Germany",
            City = "Berlin",
            RequestedUrl = "https://example.com",
            Extras = "{}"
        };

        var overlongCity = new string('C', 300);
        var overlongCountry = new string('K', 300);

        log.UpdateLocation(overlongCity, overlongCountry);

        log.City.Length.Should().Be(RequestLog.MaxCityLength);
        log.Country.Length.Should().Be(RequestLog.MaxCountryLength);
    }
}
