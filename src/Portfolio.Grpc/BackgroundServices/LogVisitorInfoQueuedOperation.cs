using System.Net;
using Portfolio.Domain.Interfaces.BackgroundServices;
using Portfolio.Infrastructure;

namespace Portfolio.Grpc.BackgroundServices;

public class LogVisitorInfoQueuedOperation : IDatabaseOperationQueueWorker
{
    public required StoreVisitorInfoRequest Request { get; init; }

    public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        if (IPAddress.TryParse(Request.IpAddress, out var ip) && IPAddress.IsLoopback(ip))
            return;

        var dbContext = serviceProvider.GetRequiredService<PortfolioDbContext>();
        
        dbContext.RequestLogs.Add(new RequestLog
        {
            OnLine              = Request.OnLine,
            Platform            = Request.Platform,
            Referrer            = Request.Referrer,
            Webdriver           = Request.Webdriver,
            UserAgent           = Request.UserAgent,
            DoNotTrack          = Request.DoNotTrack,
            Connection          = Request.Connection,
            Resolution          = Request.Resolution,
            AcceptLanguage      = Request.Language,
            DeviceMemory        = Request.DeviceMemory,
            CookieEnabled       = Request.CookieEnabled,
            MaxTouchPoints      = Request.MaxTouchPoints,
            HardwareConcurrency = Request.HardwareConcurrency,

            City         = string.Empty,
            Country      = string.Empty,
            Extras       = Request.Extras,
            ClientIp     = Request.IpAddress,
            RequestedUrl = Request.RequestedUrl
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
