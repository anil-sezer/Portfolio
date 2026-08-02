using Portfolio.Ui.Services;

namespace Portfolio.Ui.Middlewares;

public class NotFoundLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, LogVisitService logVisitService, IHttpContextAccessor httpContextAccessor)
    {
        await next(context);

        if (context.Response.StatusCode is 404 or 302)
        {
            Log.Information("🚫 Not Found: {RequestedUrl}", context.Request.Path.ToString());

            try
            {
                await logVisitService.LogVisitToWebpageAsync(httpContextAccessor);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to log visit for 404/302 response");
            }
        }
    }
}
