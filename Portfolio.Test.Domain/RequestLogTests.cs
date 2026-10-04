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
    [InlineData("\t", "\t")]
    [InlineData("\r\n", "\r\n")]
    [InlineData(null, null)]
    [InlineData(null, "")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, "   ")]
    [InlineData("\t", null)]
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

    [Fact]
    public void UpdateLocation_WhenOnlyCityProvided_UpdatesSuccessfully()
    {
        // Arrange
        var log = CreateDefaultRequestLog();

        // Act
        log.UpdateLocation("Istanbul", "");

        // Assert
        log.City.Should().Be("Istanbul");
        log.Country.Should().Be(string.Empty);
    }

    [Fact]
    public void UpdateLocation_WhenOnlyCountryProvided_UpdatesSuccessfully()
    {
        // Arrange
        var log = CreateDefaultRequestLog();

        // Act
        log.UpdateLocation("", "Turkey");

        // Assert
        log.City.Should().Be(string.Empty);
        log.Country.Should().Be("Turkey");
    }

    [Fact]
    public void UpdateLocation_WhenBothCityAndCountryProvided_UpdatesBoth()
    {
        // Arrange
        var log = CreateDefaultRequestLog();

        // Act
        log.UpdateLocation("Berlin", "Germany");

        // Assert
        log.City.Should().Be("Berlin");
        log.Country.Should().Be("Germany");
    }

    [Fact]
    public void UpdateLocation_WhenCalledConsecutively_UpdatesToLatestValues()
    {
        // Arrange
        var log = CreateDefaultRequestLog();

        // Act
        log.UpdateLocation("Istanbul", "Turkey");
        log.UpdateLocation("Berlin", "Germany");

        // Assert
        log.City.Should().Be("Berlin");
        log.Country.Should().Be("Germany");
    }

    [Fact]
    public void UpdateLocation_WhenWhitespaceInOneParameterAndValidInOther_UpdatesSuccessfully()
    {
        // Arrange
        var log = CreateDefaultRequestLog();

        // Act
        log.UpdateLocation("Ankara", "   ");

        // Assert
        log.City.Should().Be("Ankara");
        log.Country.Should().Be("   ");
    }
}
