using Portfolio.Infrastructure.Constants;

namespace Portfolio.Ui.Extensions;

public static class HttpContextExtensions
{
    public static string GetClientIpAddress(this HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return "unknown";
        }

        if (httpContext.Request.Headers.TryGetValue(HeaderConstants.XForwardedFor, out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ip = forwardedFor.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(ip))
            {
                return ip;
            }
        }

        if (httpContext.Request.Headers.TryGetValue(HeaderConstants.XRealIp, out var realIp) && !string.IsNullOrWhiteSpace(realIp))
        {
            return realIp.ToString().Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public static string GetClientIpAddress(this IHttpContextAccessor? httpContextAccessor)
    {
        return httpContextAccessor?.HttpContext.GetClientIpAddress() ?? "unknown";
    }
}
