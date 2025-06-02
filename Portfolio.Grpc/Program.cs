using DotNetEnv;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Portfolio.Domain.Interfaces.Repositories;
using Portfolio.Domain.Interfaces.ThirdPartyServices;
using Portfolio.Grpc.Services;
using Portfolio.Grpc.Services.SendEmailToAdmin;
using Portfolio.Grpc.Services.SendEmailToAdmin.Providers;
using Portfolio.Grpc.Services.VisitorInsightsServices;
using Portfolio.Infrastructure;
using Portfolio.Infrastructure.Extensions;
using Portfolio.Infrastructure.Repositories;
using Portfolio.Infrastructure.ThirdPartyServices;

var builder = WebApplication.CreateBuilder(args);

builder.InitLogsWithSerilog();
builder.InitOpenTelemetry();

if (builder.Environment.IsDevelopment())
    Env.Load("../.env");

EnvironmentExtensions.VerifyEnvironmentValuesAreSet([
    EnvironmentVariableNames.SqlDb_Host, 
    EnvironmentVariableNames.SqlDb_Port, 
    EnvironmentVariableNames.SqlDb_User, 
    EnvironmentVariableNames.SqlDb_Password, 
    EnvironmentVariableNames.SqlDb_Name,
    EnvironmentVariableNames.OpenTelemetry_CollectorEndpoint,
    EnvironmentVariableNames.DevOrProd,
    EnvironmentVariableNames.Email_Slack_WebhookUrl
]);

builder.InitDbWithPostgres();

builder.AddHealthChecksForEndpointAndDb();

builder.Services.AddGrpc();

// Repositories
builder.Services.AddScoped<IImageOfTheDayRepository, ImageOfTheDayRepository>();
builder.Services.AddScoped<IRequestLogRepository, RequestLogRepository>();
builder.Services.AddScoped<IEmailToAdminRepository, EmailToAdminRepository>();

// Factories
builder.Services.AddScoped<IEmailProvider, SlackEmailProvider>();
builder.Services.AddScoped<IEmailProviderFactory, EmailProviderFactory>();

var app = builder.Build();

app.MapLivenessHealthCheck();
app.MapReadinessHealthCheck();

// Configure the HTTP request pipeline.
app.MapGrpcService<GetBackgroundImageService>();
app.MapGrpcService<VisitorInsightsService>();
app.MapGrpcService<SendEmailToAdminService>();


app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

try
{
    Log.Information("✅ App Starting");
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