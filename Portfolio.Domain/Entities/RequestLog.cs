using Microsoft.EntityFrameworkCore;

namespace Portfolio.Domain.Entities;


// todo: DB sütunlarına belli değerlerde olma zorunluluğu vs şeyleri bu katmanda ekleyebilir misin? Oku!
// https://docs.microsoft.com/en-us/ef/core/modeling/indexes?tabs=data-annotations
// [Table("Request")]
[Comment("Accesses to the website")]
public class RequestLog : EntityBase
{
    // By Javascript
    public required string UserAgent { get; init; }
    public required string AcceptLanguage { get; init; }
    public required string Platform { get; init; }
    public required bool Webdriver { get; init; }
    public required string DeviceMemory { get; init; }
    public required string HardwareConcurrency { get; init; }
    public required int MaxTouchPoints { get; init; }
    public required string DoNotTrack { get; init; }
    public required string Connection { get; init; }
    public required bool CookieEnabled { get; init; }
    public required bool OnLine { get; init; }
    public required string Referrer { get; init; }
    public required string Resolution { get; init; }
    
    // By HttpContext & Ip check cronjob
    public required string ClientIp { get; init; }
    public required string Country { get; set; }
    public required string City { get; set; }
    
    public required string Extras { get; init; }
    
    public void UpdateLocation(string city, string country)
    {
        if (string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(country))
            throw new InvalidOperationException($"Both {nameof(City)} and {nameof(Country)} cannot be empty when updating location.");

        City = city;
        Country = country;
    }
}