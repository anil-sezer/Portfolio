using Portfolio.Ui.Services;

namespace Portfolio.Ui.Middlewares;

public class NotFoundLoggingMiddleware(RequestDelegate next, ILogger<NotFoundLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, LogVisitService logVisitService, IHttpContextAccessor httpContextAccessor)
    {
        await next(context);

        if (context.Response.StatusCode is 404 or 302)
        {
            logger.LogInformation("🚫 Not Found: {RequestedUrl}", context.Request.Path.ToString());

            try
            {
                await logVisitService.LogVisitToWebpageAsync(httpContextAccessor, context.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                // Request was aborted by the client, ignore
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to log visit for 404/302 response");
            }
        }
    }
}
