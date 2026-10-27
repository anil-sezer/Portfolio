using Portfolio.Grpc.BackgroundServices;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService
{
    public override async Task<Empty> StoreVisitorInfo(StoreVisitorInfoRequest request, ServerCallContext context)
    {
        logger.LogInformation("Visitor insights recorded for path: {RequestedUrl}", request.RequestedUrl);
        
        await DatabaseOperationQueueWorker.EnqueueAsync(new LogVisitorInfoQueuedOperation{Request = request}, context.CancellationToken);
        
        return new Empty();
    }
}