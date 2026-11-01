using System.Net;
using System.Threading.Channels;
using Grpc.Core;
using Portfolio.Grpc.BackgroundServices;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService
{
    public override async Task<Empty> StoreVisitorInfo(StoreVisitorInfoRequest request, ServerCallContext context)
    {
        if (IPAddress.TryParse(request.IpAddress, out var ip) && IPAddress.IsLoopback(ip))
        {
            logger.LogDebug("Skipping visitor insights queue for loopback address: {IpAddress}", request.IpAddress);
            return new Empty();
        }

        logger.LogInformation("Visitor insights recorded for path: {RequestedUrl}", request.RequestedUrl);

        try
        {
            await queue.EnqueueAsync(new LogVisitorInfoQueuedOperation { Request = request }, context.CancellationToken);
        }
        catch (ChannelClosedException)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, "Server is shutting down; unable to queue visitor info."));
        }

        return new Empty();
    }
}
