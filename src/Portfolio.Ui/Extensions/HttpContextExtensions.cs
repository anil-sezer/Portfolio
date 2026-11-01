namespace Portfolio.Ui.Extensions;

public static class HttpContextExtensions
{
    public static string GetClientIpAddress(this HttpContext? httpContext)
    {
        if (httpContext is null)
            return "unknown";

        var remoteIp = httpContext.Connection.RemoteIpAddress;
        if (remoteIp is null)
            return "unknown";

        if (remoteIp.IsIPv4MappedToIPv6)
            remoteIp = remoteIp.MapToIPv4();

        return remoteIp.ToString();
    }

    public static string GetClientIpAddress(this IHttpContextAccessor? httpContextAccessor)
    {
        return httpContextAccessor?.HttpContext.GetClientIpAddress() ?? "unknown";
    }
}
