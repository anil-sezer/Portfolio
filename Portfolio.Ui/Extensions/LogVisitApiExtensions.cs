using Portfolio.Ui.Services;

namespace Portfolio.Ui.Extensions;

public static class LogVisitApiExtensions
{
    public static void DefineLogVisitRoute(this WebApplication app)
    {
        app.MapPost("/api/visits/log", LogVisit)
            .WithName("LogVisit");
    }

    private static async Task<IResult> LogVisit(
        Dictionary<string, string> viaJavascript,
        LogVisitService logVisitService,
        IHttpContextAccessor httpContextAccessor)
    {
        try
        {
            await logVisitService.LogVisitToWebpageAsync(viaJavascript, httpContextAccessor);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to log visit info via API");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
