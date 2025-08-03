using Portfolio.Domain.Interfaces.BackgroundServices;
using Portfolio.Domain.Interfaces.Repositories;

namespace Portfolio.Grpc.BackgroundServices;

public class LogVisitorInfoQueuedOperation : IDatabaseOperationQueueWorker
{
    public required StoreVisitorInfoRequest Request { get; init; }

    public async Task ExecuteAsync(IServiceProvider serviceProvider)
    {
        var repo = serviceProvider.GetRequiredService<IRequestLogRepository>();
        
        await repo.CreateAsync(new RequestLog
        {
            AcceptLanguage = Request.Language,
            UserAgent = Request.UserAgent,
            Platform = Request.Platform,
            Referrer = Request.Referrer,
            DoNotTrack = Request.DoNotTrack,
            Connection = Request.Connection,
            Resolution = Request.Resolution,
            DeviceMemory = Request.DeviceMemory,
            OnLine = Request.OnLine,
            HardwareConcurrency = Request.HardwareConcurrency,
            Webdriver = Request.Webdriver,
            CookieEnabled = Request.CookieEnabled,
            MaxTouchPoints = Request.MaxTouchPoints,

            ClientIp = Request.IpAddress,
            City = string.Empty,
            Country = string.Empty,
            Extras = Request.Extras
        });
    }
}
