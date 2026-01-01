using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Portfolio.Infrastructure.Helpers;
using Serilog;
using ExportProcessorType = OpenTelemetry.ExportProcessorType;

namespace Portfolio.Infrastructure.Extensions;

public static class OpenTelemetryExtensions
{
    public static void InitOpenTelemetry(this WebApplicationBuilder builder, string sourceName)
    {
        const string serviceVersion = "1.0.0";

        var serviceName = AssemblyHelper.GetStartupProjectsName();
        Action<ResourceBuilder> appResourceBuilder =
            resource => resource
                .AddContainerDetector()
                .AddHostDetector()
                .AddOperatingSystemDetector()
                .AddService(serviceName, serviceVersion: serviceVersion, serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = EnvVars.ASPNETCORE_ENVIRONMENT
                });

        var otelEndpoint = EnvVars.OTEL_COLLECTOR_ENDPOINT;
        if (string.IsNullOrEmpty(otelEndpoint))
        {
            Log.Error("Otel endpoint not set at environment variable! Please set environment variable {OtelEndpoint}", EnvVars.OTEL_COLLECTOR_ENDPOINT);
            return;
        }
        
        Log.Information("Starting Open Telemetry with endpoint: {OtelEndpoint}", otelEndpoint);
        
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(appResourceBuilder)
            .WithTracing(tracerBuilder => tracerBuilder
                .AddSource(serviceName)
                .SetSampler(new TraceIdRatioBasedSampler(1.0))
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("http.request.body.size", request.ContentLength);
                        activity.SetTag("user.id", request.HttpContext.User?.Identity?.Name);
                    };
                    options.EnrichWithHttpResponse = (activity, response) =>
                    {
                        activity.SetTag("http.response.body.size", response.ContentLength);
                    };
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.SetDbStatementForStoredProcedure = true;
                    options.EnrichWithIDbCommand = (activity, command) =>
                    {
                        activity.SetTag("db.command.timeout", command.CommandTimeout);
                    };
                })
                .AddGrpcCoreInstrumentation()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(otelEndpoint);
                    o.Protocol = OtlpExportProtocol.Grpc;
                    o.ExportProcessorType = ExportProcessorType.Batch;
                    o.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<System.Diagnostics.Activity>
                    {
                        MaxQueueSize = 2048,
                        ScheduledDelayMilliseconds = 5000,
                        ExporterTimeoutMilliseconds = 30000,
                        MaxExportBatchSize = 512
                    };
                })
                // .AddConsoleExporter()
            ) 
            .WithMetrics(meterBuilder => meterBuilder
                    .AddMeter(serviceName)
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .SetExemplarFilter(ExemplarFilterType.TraceBased) // todo: What is this ask LLM
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(otelEndpoint);
                        o.Protocol = OtlpExportProtocol.Grpc;
                        o.ExportProcessorType = ExportProcessorType.Batch;
                        o.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<System.Diagnostics.Activity>
                        {
                            MaxQueueSize = 2048,
                            ScheduledDelayMilliseconds = 5000,
                            ExporterTimeoutMilliseconds = 30000,
                            MaxExportBatchSize = 512
                        };
                    })
                // .AddConsoleExporter()
            );
    }
}