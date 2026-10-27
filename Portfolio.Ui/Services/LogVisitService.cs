using Portfolio.Ui.Factories;

namespace Portfolio.Ui.Services;

public class LogVisitService(VisitorInsights.VisitorInsightsClient visitorInsightsClient, ILogger<LogVisitService> logger)
{
    public async Task LogVisitToWebpageAsync(IHttpContextAccessor httpContextAccessor, CancellationToken cancellationToken = default)
    {
        var request = ClientInfoRequestFactory.Create(httpContextAccessor);

        await LogVisitToGrpcAsync(request, cancellationToken);
    }
    
    public async Task LogVisitToWebpageAsync(Dictionary<string, string> viaJavascript, IHttpContextAccessor httpContextAccessor, CancellationToken cancellationToken = default)
    {
        var request = ClientInfoRequestFactory.Create(viaJavascript, httpContextAccessor);

        await LogVisitToGrpcAsync(request, cancellationToken);
    }

    private async Task LogVisitToGrpcAsync(StoreVisitorInfoRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await visitorInsightsClient.StoreVisitorInfoAsync(request, cancellationToken: cancellationToken);
            logger.LogInformation("📨 Sent a gRPC request to {ServiceName}", nameof(VisitorInsights.VisitorInsightsClient.StoreVisitorInfoAsync));
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Logging visit to gRPC was cancelled");
        }
        catch (Exception e)
        {
            logger.LogError("❌ Looks like gRPC pod is down. I currently cannot store insights about internet now. Error: {ExMsg}", e.Message);
        }
    }
}