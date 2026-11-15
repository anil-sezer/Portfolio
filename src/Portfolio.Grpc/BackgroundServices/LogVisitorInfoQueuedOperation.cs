using System.Net;
using Portfolio.Domain.Interfaces.BackgroundServices;
using Portfolio.Domain.Utilities;
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

        var isBot = BotDetector.IsBot(new BotDetectionContext
        {
            UserAgent           = Request.UserAgent,
            Webdriver           = Request.Webdriver,
            RequestedUrl        = Request.RequestedUrl,
            Platform            = Request.Platform,
            AcceptLanguage      = Request.Language,
            Resolution          = Request.Resolution,
            Connection          = Request.Connection,
            DeviceMemory        = Request.DeviceMemory,
            HardwareConcurrency = Request.HardwareConcurrency,
            MaxTouchPoints      = Request.MaxTouchPoints,
            CookieEnabled       = Request.CookieEnabled,
            OnLine              = Request.OnLine,
            Extras              = Request.Extras
        });
        
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
            IsBot               = isBot,

            City         = string.Empty,
            Country      = string.Empty,
            Extras       = Request.Extras,
            ClientIp     = Request.IpAddress,
            RequestedUrl = Request.RequestedUrl
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
