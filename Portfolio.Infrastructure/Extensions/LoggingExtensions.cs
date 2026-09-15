using Microsoft.AspNetCore.Builder;
using Portfolio.Infrastructure.Helpers;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace Portfolio.Infrastructure.Extensions;

public static class LoggingExtensions
{
    public static void InitLogsWithSerilog(this WebApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, lc) =>
        {
            lc.ReadFrom.Services(services) // enables DI-aware enrichers/sinks
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .MinimumLevel.Override("Grpc", LogEventLevel.Warning)
                .Enrich.FromLogContext() // For UseSerilogRequestLogging?
                .WriteTo.Console();

            if (!EnvVars.IsDevelopment())
            {
                lc.WriteTo.OpenTelemetry(o =>
                {
                    o.Endpoint = $"{EnvVars.OTEL_COLLECTOR_ENDPOINT.TrimEnd('/')}/v1/logs";
                    o.Protocol = OtlpProtocol.HttpProtobuf;
                    o.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = AssemblyHelper.GetServiceName()
                    };
                });
            }
        });
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
                if (!string.IsNullOrEmpty(path) && (AssetHelper.IsStaticAsset(path) || HealthCheckExtensions.IsThisPathHealthCheck(path)))
                    return LogEventLevel.Verbose;
                
                // 3. Log normal page/API requests as Information
                return LogEventLevel.Information;
            };
        });
    }
}