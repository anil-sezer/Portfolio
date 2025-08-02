using DotNetEnv;
using Portfolio.Domain.Interfaces.Repositories;
using Portfolio.Domain.Interfaces.ThirdPartyServices;
using Portfolio.Grpc.Services;
using Portfolio.Grpc.Services.SendEmailToAdmin;
using Portfolio.Grpc.Services.SendEmailToAdmin.Providers;
using Portfolio.Grpc.Services.VisitorInsightsServices;
using Portfolio.Infrastructure.Extensions;
using Portfolio.Infrastructure.Repositories;
using Portfolio.Infrastructure.ThirdPartyServices;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
    Env.Load("../.env");

// Trigger a get value to ensure that the env vars are loaded
_ = EnvVars.ASPNETCORE_ENVIRONMENT;

_ = EnvVars.SQL_DB_HOST;
_ = EnvVars.SQL_DB_PORT;
_ = EnvVars.SQL_DB_USER;
_ = EnvVars.SQL_DB_NAME;
_ = EnvVars.SQL_DB_PASSWORD;

_ = EnvVars.OTEL_COLLECTOR_ENDPOINT;
_ = EnvVars.NOTIFICATION_TELEGRAM_API_KEY;
_ = EnvVars.NOTIFICATION_TELEGRAM_CHAT_ID;

builder.InitLogsWithSerilog();
builder.InitOpenTelemetry();

builder.InitDbWithPostgres();

builder.AddHealthChecksForEndpointAndDb();

builder.Services.AddGrpc();

// Repositories
builder.Services.AddScoped<IImageOfTheDayRepository, ImageOfTheDayRepository>();
builder.Services.AddScoped<IRequestLogRepository, RequestLogRepository>();
builder.Services.AddScoped<INotificationToAdminRepository, NotificationToAdminRepository>();

// Factories
builder.Services.AddScoped<INotificationProvider, NotificationProviderTelegram>();
builder.Services.AddScoped<INotificationProviderFactory, NotificationProviderFactory>();

var app = builder.Build();

app.MapLivenessHealthCheck();
app.MapReadinessHealthCheck();

// Configure the HTTP request pipeline.
app.MapGrpcService<GetBackgroundImageService>();
app.MapGrpcService<VisitorInsightsService>();
app.MapGrpcService<SendNotificationToAdminService>();


app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

try
{
    Log.Information("✅ App Starting");
    app.AutoMigrateInDevEnv();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ The app failed to start");
}
finally
{
    Log.CloseAndFlush();
}