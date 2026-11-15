using System.Text.Json;
using FluentAssertions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Utilities;

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

    [Fact]
    public void Extras_WhenOversizedJsonWithHugeValue_RemainsValidJsonWithinLimit()
    {
        var oversizedJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["Host"] = "anilsezer.net",
            ["X-Huge"] = new string('A', 5000)
        });

        var log = new RequestLog
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
            Extras = oversizedJson
        };

        log.Extras.Length.Should().BeLessThanOrEqualTo(RequestLog.MaxExtrasLength);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(log.Extras)!;
        parsed["Host"].Should().Be("anilsezer.net");
        parsed["X-Huge"].Length.Should().Be(BoundedJsonSerializer.DefaultMaxValueLength);
    }

    [Fact]
    public void Extras_WhenOversizedJsonWithManyKeys_RemainsValidJsonWithinLimit()
    {
        var manyEntries = Enumerable.Range(0, 30)
            .ToDictionary(i => $"X-Header-{i:D2}", i => new string((char)('a' + (i % 26)), 300));
        var oversizedJson = JsonSerializer.Serialize(manyEntries);

        var log = new RequestLog
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
            Extras = oversizedJson
        };

        log.Extras.Length.Should().BeLessThanOrEqualTo(RequestLog.MaxExtrasLength);
        var act = () => JsonDocument.Parse(log.Extras);
        act.Should().NotThrow();
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(log.Extras)!;
        parsed.Should().NotBeEmpty();
        parsed.Count.Should().BeLessThan(30);
    }
}
