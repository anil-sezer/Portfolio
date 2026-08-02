using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

namespace Portfolio.Infrastructure.Extensions;

public static class LoggingExtensions
{
    // todo: send this config to chatgpt for asking what minimumlevel does. And after, add this for prod env: https://github.com/b00ted/serilog-sinks-postgresql
    public static void InitLogsWithSerilog(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console()
            // .WriteTo.OpenTelemetry(
            //     endpoint: "http://127.0.0.1:4318/v1/logs",
            //     protocol: OtlpProtocol.Grpc
            //     )
            .CreateLogger();
        builder.Host.UseSerilog();
    }

    public static void SetupRequestLoggingForBlazor(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                // 1. Log server errors or unhandled exceptions normally
                if (ex != null || httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;
                // 2. Ignore static asset requests (set level to Verbose so they don't print to console)
                var path = httpContext.Request.Path.Value;
                if (!string.IsNullOrEmpty(path) && (IsStaticAsset(path) || HealthCheckExtensions.IsThisPathHealthCheck(path)))
                    return LogEventLevel.Verbose;
                // 3. Log normal page/API requests as Information
                return LogEventLevel.Information;
            };
        });
    }
    
    private static bool IsStaticAsset(string path)
    {
        return path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase);
    }
}