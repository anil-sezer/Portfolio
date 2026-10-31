using System.Globalization;
using System.Threading.RateLimiting;

namespace Portfolio.Ui.Extensions;

public static class RateLimiterExtensions
{
    public const string EmailSendPolicy = "EmailSendPolicy";
    public const string VisitLogPolicy = "VisitLogPolicy";
    public const string ClusterStatsPolicy = "ClusterStatsPolicy";
    public const string HeroImagePolicy = "HeroImagePolicy";
    public const string BlazorPagesPolicy = "BlazorPagesPolicy";

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((long)Math.Ceiling(retryAfter.TotalSeconds)).ToString(NumberFormatInfo.InvariantInfo);
                }

                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken);
            };

            options.AddPolicy(EmailSendPolicy, httpContext =>
            {
                var clientIp = httpContext.GetClientIpAddress();
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(VisitLogPolicy, httpContext =>
            {
                var clientIp = httpContext.GetClientIpAddress();
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(ClusterStatsPolicy, httpContext =>
            {
                var clientIp = httpContext.GetClientIpAddress();
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(HeroImagePolicy, httpContext =>
            {
                var clientIp = httpContext.GetClientIpAddress();
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(BlazorPagesPolicy, httpContext =>
            {
                var clientIp = httpContext.GetClientIpAddress();
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });

        return services;
    }
}
