using System.Text.Json;
using Portfolio.Grpc.BackgroundServices;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService
{
    public override async Task<Empty> StoreVisitorInfo(StoreVisitorInfoRequest r, ServerCallContext context)
    {
        Log.Information("Request to log: {Log}",JsonSerializer.Serialize(r));
        
        await DatabaseOperationQueueWorker.EnqueueAsync(new LogVisitorInfoQueuedOperation{Request = r});
        
        return new Empty();
    }
}