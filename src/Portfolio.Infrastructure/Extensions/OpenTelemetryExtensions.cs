using System.Diagnostics.Tracing;
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
    public static void InitOpenTelemetry(this WebApplicationBuilder builder)
    {
        if (EnvVars.IsDevelopment())
        {
            Log.Information("Skipping OpenTelemetry initialization in Development environment for faster startup");
            return;
        }

        const string serviceVersion = "1.0.0";

        // Enable if you wanna debug OpenTelemetry. Listens to internal events.
        // if (EnvVars.IsDevelopment())
        //     _ = new OtelDiagnosticListener();

        var serviceName = AssemblyHelper.GetServiceName();
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
                .AddAspNetCoreInstrumentation(o =>
                {
                    o.RecordException = true;
                    o.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("http.request.body.size", request.ContentLength);
                        activity.SetTag("user.id", request.HttpContext.User.Identity?.Name);
                    };
                    o.EnrichWithHttpResponse = (activity, response) =>
                    {
                        activity.SetTag("http.response.body.size", response.ContentLength);
                    };
                    o.Filter = httpContext =>
                    {
                        var path = httpContext.Request.Path.Value;
                        if (string.IsNullOrEmpty(path)) return true;

                        if (path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase))
                            return false;

                        return !HealthCheckExtensions.IsThisPathHealthCheck(path) && !AssetHelper.IsStaticAsset(path);
                    };
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(o =>
                {
                    o.SetDbStatementForText = true;
                    o.SetDbStatementForStoredProcedure = true;
                    o.EnrichWithIDbCommand = (activity, command) =>
                    {
                        activity.SetTag("db.command.timeout", command.CommandTimeout);
                    };
                })
                .AddGrpcCoreInstrumentation()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri($"{otelEndpoint.TrimEnd('/')}/v1/traces");
                    o.Protocol = OtlpExportProtocol.HttpProtobuf;
                    o.ExportProcessorType = ExportProcessorType.Batch;
                    o.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<System.Diagnostics.Activity>
                    {
                        MaxQueueSize = 2048,
                        ScheduledDelayMilliseconds = 5000,
                        ExporterTimeoutMilliseconds = 30000,
                        MaxExportBatchSize = 512
                    };
                })
            )
            .WithMetrics(meterBuilder => meterBuilder
                .AddMeter(serviceName)
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddAspNetCoreInstrumentation()
                .SetExemplarFilter(ExemplarFilterType.TraceBased)
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri($"{otelEndpoint.TrimEnd('/')}/v1/metrics");
                    o.Protocol = OtlpExportProtocol.HttpProtobuf;
                    o.ExportProcessorType = ExportProcessorType.Batch;
                    o.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<System.Diagnostics.Activity>
                    {
                        MaxQueueSize = 2048,
                        ScheduledDelayMilliseconds = 5000,
                        ExporterTimeoutMilliseconds = 30000,
                        MaxExportBatchSize = 512
                    };
                })
            );
    }
}

internal class OtelDiagnosticListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name.StartsWith("OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.Message == null) 
            return;
        
        try
        {
            var payload = eventData.Payload != null ? eventData.Payload.ToArray() : [];
            var message = string.Format(eventData.Message, payload);
            Log.Warning("[OTEL DIAGNOSTIC] {Message}", message);
        }
        catch
        {
            Log.Warning("[OTEL DIAGNOSTIC] {EventName} - {Message}", eventData.EventName, eventData.Message);
        }
    }
}