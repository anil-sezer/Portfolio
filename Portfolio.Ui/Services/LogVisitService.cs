using Portfolio.Ui.Factories;

namespace Portfolio.Ui.Services;

public class LogVisitService(VisitorInsights.VisitorInsightsClient visitorInsightsClient)
{
    public async Task LogVisitToWebpageAsync(IHttpContextAccessor httpContextAccessor)
    {
        var request = ClientInfoRequestFactory.Create(httpContextAccessor);

        await LogVisitToGrpcAsync(request);
    }
    
    public async Task LogVisitToWebpageAsync(Dictionary<string, string> viaJavascript, IHttpContextAccessor httpContextAccessor)
    {
        var request = ClientInfoRequestFactory.Create(viaJavascript, httpContextAccessor);

        await LogVisitToGrpcAsync(request);
    }

    private async Task LogVisitToGrpcAsync(StoreVisitorInfoRequest request)
    {
        try
        {
            await visitorInsightsClient.StoreVisitorInfoAsync(request);
            Log.Information("📨 Sent a gRPC request to {ServiceName}", nameof(VisitorInsights.VisitorInsightsClient.StoreVisitorInfoAsync));
        }
        catch (Exception e)
        {
            Log.Error("❌ Looks like gRPC pod is down. I currently cannot store insights about internet now. Error: {ExMsg}", e.Message);
        }
    }
}