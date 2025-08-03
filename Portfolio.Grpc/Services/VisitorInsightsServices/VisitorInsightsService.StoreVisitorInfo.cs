using System.Text.Json;
using Portfolio.Grpc.BackgroundServices;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService
{
    public override async Task<Empty> StoreVisitorInfo(StoreVisitorInfoRequest r, ServerCallContext context)
    {
        Log.Information("Request to log: {Log}",JsonSerializer.Serialize(r));
        
        await DatabaseOperationQueueWorker.EnqueueAsync(new LogVisitorInfoQueuedOperation{Request = r});

        // await requestLogRepository.CreateAsync(
        //     new RequestLog
        //     {
        //         AcceptLanguage = r.Language,
        //         UserAgent = r.UserAgent,
        //         Platform = r.Platform,
        //         Referrer = r.Referrer,
        //         DoNotTrack = r.DoNotTrack,
        //         Connection = r.Connection,
        //         Resolution = r.Resolution,
        //         DeviceMemory = r.DeviceMemory,
        //         OnLine = r.OnLine,
        //         HardwareConcurrency = r.HardwareConcurrency,
        //         Webdriver = r.Webdriver,
        //         CookieEnabled = r.CookieEnabled,
        //         MaxTouchPoints = r.MaxTouchPoints,
        //
        //         ClientIp = r.IpAddress,
        //         City = string.Empty,
        //         Country = string.Empty,
        //         Extras = r.Extras
        //     }
        // );
        
        return new Empty();
    }
}