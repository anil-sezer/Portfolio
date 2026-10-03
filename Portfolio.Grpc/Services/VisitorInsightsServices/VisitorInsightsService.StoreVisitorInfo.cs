using System.Text.Json;
using Portfolio.Grpc.BackgroundServices;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService
{
    public override async Task<Empty> StoreVisitorInfo(StoreVisitorInfoRequest request, ServerCallContext context)
    {
        Log.Information("Request to log: {Log}",JsonSerializer.Serialize(request));
        
        await DatabaseOperationQueueWorker.EnqueueAsync(new LogVisitorInfoQueuedOperation{Request = request}, context.CancellationToken);
        
        return new Empty();
    }
}