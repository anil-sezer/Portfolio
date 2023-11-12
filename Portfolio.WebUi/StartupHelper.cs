using Microsoft.EntityFrameworkCore;
using Nest;
using Portfolio.DataAccess;
using Portfolio.Domain.Helpers;
using Serilog;
using Serilog.Events;

namespace Portfolio.WebUi;

public static class StartupHelper
{
    public static void InitLogsWithSerilog(this WebApplicationBuilder builder)
    {
        using var log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .WriteTo.Console()
            .CreateLogger();
        builder.Host.UseSerilog(log);
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