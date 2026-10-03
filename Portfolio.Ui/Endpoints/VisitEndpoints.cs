using Portfolio.Ui.Extensions;
using Portfolio.Ui.Services;

namespace Portfolio.Ui.Endpoints;

public static class VisitEndpoints
{
    public const string Route = "/api/visits/log";

    public static void MapVisitEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(Route, LogVisit)
            .WithName("LogVisit")
            .RequireAntiforgeryToken()
            .RequireRateLimiting(RateLimiterExtensions.VisitLogPolicy);
    }

    private static async Task<IResult> LogVisit(
        Dictionary<string, string> viaJavascript,
        LogVisitService logVisitService,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        try
        {
            await logVisitService.LogVisitToWebpageAsync(viaJavascript, httpContextAccessor, cancellationToken);
            return Results.Ok();
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499); // Client Closed Request
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to log visit info via API");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
