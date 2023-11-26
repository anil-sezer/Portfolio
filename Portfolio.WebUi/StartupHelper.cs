using Microsoft.EntityFrameworkCore;
using Nest;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Portfolio.DataAccess;
using Serilog;
using Serilog.Events;

namespace Portfolio.WebUi;

public static class StartupHelper
{
    public static void InitLogsWithSerilog(this WebApplicationBuilder builder)
    {
        // todo-anil: Is logging to OpenTelemetry is working? How should I debug?
        using var log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .WriteTo.Console()
            .WriteTo.OpenTelemetry(options =>
            {
                // options.Endpoint = "http://localhost:4317/v1/logs";
                // options.Protocol = OtlpProtocol.HttpProtobuf;
            })
            .CreateLogger();
        builder.Host.UseSerilog(log);
    }
    
    public static void SetupOpenTelemetry(this WebApplicationBuilder builder)
    {
        // todo-anil-beforeMerge: Get the version from somewhere else. Maybe a file?
        var serviceName = "Portfolio.WebUi";
        var serviceVersion = "1.0.0";

        builder.Logging.AddOpenTelemetry(options =>
        {
            options
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                )
                .AddOtlpExporter(opt =>
                {
                    // opt.Endpoint = new Uri("http://localhost:4317");
                    // opt.Protocol = OtlpExportProtocol.Grpc; // or OtlpExportProtocol.Grpc
                });
            //.AddConsoleExporter()
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(
                resource => resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.SetDbStatementForStoredProcedure = true;
                })
                // .AddConsoleExporter()
                .AddOtlpExporter(opt =>
                {
                    // opt.Endpoint = new Uri("http://localhost:4317");
                    // opt.Protocol = OtlpExportProtocol.Grpc;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                // .AddConsoleExporter()
                .AddOtlpExporter(opt =>
                {
                    // opt.Endpoint = new Uri("http://localhost:4317");
                    // opt.Protocol = OtlpExportProtocol.Grpc;
                }));
    }

    public static void DbInitWithPostgres(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<WebAppDbContext>(options =>
                options
                    .UseNpgsql(connectionString)
                    .UseSnakeCaseNamingConvention()
                    .EnableSensitiveDataLogging() // todo: Only for development
                    .EnableDetailedErrors() // todo: Only for development
        );
    }
    
    public static void DbInitWithElastic(this IServiceCollection services)
    {
        var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
            .DefaultIndex("my_default_index");
        var client = new ElasticClient(settings);

        services.AddSingleton<IElasticClient>(client);
    }
    
    // public static void DbInitWithSqLite(this IServiceCollection services)
    // {
    //     const string connectionString = "Data Source=WebApp.db";
    //     // var connectionString = "Data Source=/app/Data/WebApp.db"; // todo: Get this from Kubernetes ENV values?
    //
    //     using var connection = new SqliteConnection(connectionString);
    //     connection.Open();
    //
    //     var command = connection.CreateCommand();
    //     command.CommandText =
    //                         @"
    //                             CREATE TABLE IF NOT EXISTS Request (
    //                                 Id TEXT PRIMARY KEY,
    //                                 CreationTime DATETIME NOT NULL,
    //                                 UserAgent TEXT NOT NULL,
    //                                 AcceptLanguage TEXT NOT NULL,
    //                                 ClientIp TEXT NOT NULL,
    //                                 DeviceType TEXT NOT NULL
    //                             );
    //                             CREATE TABLE IF NOT EXISTS BingDailyBackground (
    //                                 Id TEXT PRIMARY KEY,
    //                                 CreationTime DATETIME NOT NULL,
    //                                 ImageUrl DATETIME NOT NULL,
    //                                 UrlWorks BOOL NOT NULL
    //                             );
    //                             CREATE TABLE IF NOT EXISTS Email (
    //                                 Id TEXT PRIMARY KEY,
    //                                 CreationTime DATETIME NOT NULL,
    //                                 Name TEXT NOT NULL,
    //                                 EmailAddress TEXT NOT NULL,
    //                                 Subject TEXT NOT NULL,
    //                                 Message TEXT NOT NULL
    //                             );
    //                             ";
    //     command.ExecuteNonQuery();
    //
    //     services.AddDbContext<WebAppDbContext>(options =>
    //         {
    //             options.UseSqlite(connectionString);
    //             // .EnableDetailedErrors(); // todo: Add this up, trigger some errors & observe the difference. It should be nicer yea?
    //             if (EnvironmentHelper.IsDevelopment())
    //                 options
    //                     .EnableSensitiveDataLogging()
    //                     .EnableDetailedErrors();
    //             // .AddInterceptors(new TaggedQueryCommandInterceptor());
    //         }
    //     );
    // }
}