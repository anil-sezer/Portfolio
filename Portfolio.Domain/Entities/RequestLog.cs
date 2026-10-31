using Portfolio.Domain.Extensions;

namespace Portfolio.Domain.Entities;

public class RequestLog : EntityBase
{
    public const int MaxUserAgentLength = 1000;
    public const int MaxAcceptLanguageLength = 255;
    public const int MaxPlatformLength = 100;
    public const int MaxDeviceMemoryLength = 32;
    public const int MaxHardwareConcurrencyLength = 32;
    public const int MaxDoNotTrackLength = 32;
    public const int MaxConnectionLength = 64;
    public const int MaxReferrerLength = 2048;
    public const int MaxResolutionLength = 64;
    public const int MaxClientIpLength = 64;
    public const int MaxCountryLength = 100;
    public const int MaxCityLength = 100;
    public const int MaxRequestedUrlLength = 2048;
    public const int MaxExtrasLength = 4000;

    // By Javascript
    private readonly string _userAgent = string.Empty;
    public required string UserAgent
    {
        get => _userAgent;
        init => _userAgent = value.Truncate(MaxUserAgentLength);
    }

    private readonly string _acceptLanguage = string.Empty;
    public required string AcceptLanguage
    {
        get => _acceptLanguage;
        init => _acceptLanguage = value.Truncate(MaxAcceptLanguageLength);
    }

    private readonly string _platform = string.Empty;
    public required string Platform
    {
        get => _platform;
        init => _platform = value.Truncate(MaxPlatformLength);
    }

    public required bool Webdriver { get; init; }

    private readonly string _deviceMemory = string.Empty;
    public required string DeviceMemory
    {
        get => _deviceMemory;
        init => _deviceMemory = value.Truncate(MaxDeviceMemoryLength);
    }

    private readonly string _hardwareConcurrency = string.Empty;
    public required string HardwareConcurrency
    {
        get => _hardwareConcurrency;
        init => _hardwareConcurrency = value.Truncate(MaxHardwareConcurrencyLength);
    }

    public required int MaxTouchPoints { get; init; }

    private readonly string _doNotTrack = string.Empty;
    public required string DoNotTrack
    {
        get => _doNotTrack;
        init => _doNotTrack = value.Truncate(MaxDoNotTrackLength);
    }

    private readonly string _connection = string.Empty;
    public required string Connection
    {
        get => _connection;
        init => _connection = value.Truncate(MaxConnectionLength);
    }

    public required bool CookieEnabled { get; init; }
    public required bool OnLine { get; init; }

    private readonly string _referrer = string.Empty;
    public required string Referrer
    {
        get => _referrer;
        init => _referrer = value.Truncate(MaxReferrerLength);
    }

    private readonly string _resolution = string.Empty;
    public required string Resolution
    {
        get => _resolution;
        init => _resolution = value.Truncate(MaxResolutionLength);
    }

    // By HttpContext & Ip check cronjob
    private readonly string _clientIp = string.Empty;
    public required string ClientIp
    {
        get => _clientIp;
        init => _clientIp = value.Truncate(MaxClientIpLength);
    }

    private string _country = string.Empty;
    public required string Country
    {
        get => _country;
        init => _country = value.Truncate(MaxCountryLength);
    }

    private string _city = string.Empty;
    public required string City
    {
        get => _city;
        init => _city = value.Truncate(MaxCityLength);
    }

    private readonly string _requestedUrl = string.Empty;
    public required string RequestedUrl
    {
        get => _requestedUrl;
        init => _requestedUrl = value.Truncate(MaxRequestedUrlLength);
    }

    private readonly string _extras = string.Empty;
    public required string Extras
    {
        get => _extras;
        init => _extras = value.Truncate(MaxExtrasLength);
    }

    public void UpdateLocation(string city, string country)
    {
        if (string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(country))
            throw new InvalidOperationException($"Both {nameof(City)} and {nameof(Country)} cannot be empty when updating location.");

        _city = city.Truncate(MaxCityLength);
        _country = country.Truncate(MaxCountryLength);
    }
}