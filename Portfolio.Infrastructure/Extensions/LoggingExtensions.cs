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
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .WriteTo.Console()
            // .WriteTo.OpenTelemetry(
            //     endpoint: "http://127.0.0.1:4318/v1/logs",
            //     protocol: OtlpProtocol.Grpc
            //     )
            .CreateLogger();
        builder.Host.UseSerilog();
    }
}