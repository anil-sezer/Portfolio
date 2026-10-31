using Microsoft.AspNetCore.Diagnostics;
using Portfolio.Ui.Services;

namespace Portfolio.Ui.Middlewares;

public class NotFoundLoggingMiddleware(RequestDelegate next, ILogger<NotFoundLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, LogVisitService logVisitService, IHttpContextAccessor httpContextAccessor)
    {
        await next(context);

        // Skip duplicate logging if this is an internal re-execution from StatusCodePagesWithReExecute
        if (context.Features.Get<IStatusCodeReExecuteFeature>() != null)
        {
            return;
        }

        if (context.Response.StatusCode == StatusCodes.Status404NotFound)
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
                logger.LogError(ex, "Failed to log visit for 404 response");
            }
        }
    }
}
