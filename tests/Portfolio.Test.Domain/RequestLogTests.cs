using FluentAssertions;
using Portfolio.Domain.Entities;

namespace Portfolio.Test.Domain;

public class RequestLogTests
{
    private static RequestLog CreateDefaultRequestLog() => new()
    {
        UserAgent = "Mozilla/5.0",
        AcceptLanguage = "en-US",
        Platform = "Win32",
        Webdriver = false,
        DeviceMemory = "8",
        HardwareConcurrency = "8",
        MaxTouchPoints = 0,
        DoNotTrack = "1",
        Connection = "wifi",
        CookieEnabled = true,
        OnLine = true,
        Referrer = "https://google.com",
        Resolution = "1920x1080",
        ClientIp = "127.0.0.1",
        Country = "DefaultCountry",
        City = "DefaultCity",
        RequestedUrl = "https://anilsezer.net",
        Extras = "{}"
    };

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, null)]
    public void UpdateLocation_WhenBothCityAndCountryAreNullOrWhitespace_ThrowsInvalidOperationException(string? city, string? country)
    {
        // Arrange
        var log = CreateDefaultRequestLog();

        // Act
        var act = () => log.UpdateLocation(city!, country!);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be empty when updating location*");
    }

    [Theory]
    [InlineData("Istanbul", "", "Istanbul", "")]
    [InlineData("", "Turkey", "", "Turkey")]
    [InlineData("Berlin", "Germany", "Berlin", "Germany")]
    [InlineData("Ankara", "   ", "Ankara", "   ")]
    public void UpdateLocation_WhenAtLeastOneParameterProvided_UpdatesSuccessfully(string city, string country, string expectedCity, string expectedCountry)
    {
        // Arrange
        var log = CreateDefaultRequestLog();

        // Act
        log.UpdateLocation(city, country);

        // Assert
        log.City.Should().Be(expectedCity);
        log.Country.Should().Be(expectedCountry);
    }
}
