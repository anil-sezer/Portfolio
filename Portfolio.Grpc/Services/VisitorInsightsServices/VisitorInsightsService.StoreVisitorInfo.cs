using System.Text.Json;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService
{
    // todo: I wanna use MediatR here
    public override async Task<Empty> StoreVisitorInfo(StoreVisitorInfoRequest r, ServerCallContext context)
    {
        Log.Information("Request to log: {Log}",JsonSerializer.Serialize(r));

        await requestLogRepository.CreateAsync(
            new RequestLog
            {
                AcceptLanguage = r.Language,
                UserAgent = r.UserAgent,
                Platform = r.Platform,
                Referrer = r.Referrer,
                DoNotTrack = r.DoNotTrack,
                Connection = r.Connection,
                Resolution = r.Resolution,
                DeviceMemory = r.DeviceMemory,
                OnLine = r.OnLine,
                HardwareConcurrency = r.HardwareConcurrency,
                Webdriver = r.Webdriver,
                CookieEnabled = r.CookieEnabled,
                MaxTouchPoints = r.MaxTouchPoints,

                ClientIp = r.IpAddress,
                City = string.Empty,
                Country = string.Empty,
                Extras = r.Extras
            }
        );
        
        return new Empty();
    }
}