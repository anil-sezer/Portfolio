using System.Net;
using DotNetEnv;
using Microsoft.AspNetCore.HttpOverrides;
using Portfolio.Ui.Components;
using Portfolio.Infrastructure.Constants;
using Portfolio.Infrastructure.Extensions;
using Portfolio.Ui.Extensions;
using Portfolio.Ui.Middlewares;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
    Env.TraversePath().Load();

builder.InitLogsWithSerilog();

EnvVars.TestEnvVariablesForFrontend();

builder.InitOpenTelemetry();

builder.Services.AddRazorComponents(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddMemoryCache();

builder.Services.AddLocalization();

builder.InitializeHealthChecks()
    .AddCheck<Portfolio.Ui.HealthChecks.GrpcBackendHealthCheck>(
        name: Portfolio.Ui.HealthChecks.GrpcBackendHealthCheck.CheckName,
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: ["ready"]);

builder.InitializeGrpcClients();

builder.InitializeServices();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.244.0.0/16"));
    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.96.0.0/12"));
    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("127.0.0.1/8"));
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = HeaderConstants.XsrfToken;
});

builder.Services.AddAppRateLimiting();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStatusCodePagesWithReExecute("/404");

app.SetupRequestLoggingForBlazor();

app.SetupLocalization();

app.UseRateLimiter();

app.UseAntiforgery();

app.UseMiddleware<NotFoundLoggingMiddleware>();

app.MapLivenessHealthCheck();
app.MapReadinessHealthCheck();
app.MapHealthCheckForUptimeRobot();
app.DefineEnglishRedirectRoute();
app.MapApiEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .RequireRateLimiting(RateLimiterExtensions.BlazorPagesPolicy);

await app.RunTheAppWithProperLoggingAsync("⭐⭐ UI Starting. Can access it from: http://localhost:5002 at dev env. ⭐⭐");

