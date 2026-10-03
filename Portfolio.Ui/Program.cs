using DotNetEnv;
using Portfolio.Ui;
using Portfolio.Ui.Components;
using Portfolio.Infrastructure.Constants;
using Portfolio.Infrastructure.Extensions;
using Portfolio.Ui.Extensions;
using Portfolio.Ui.Middlewares;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
    Env.Load("../.env");

builder.InitLogsWithSerilog();

EnvVars.TestEnvVariablesForFrontend();

builder.InitOpenTelemetry();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

builder.Services.AddMemoryCache();

builder.Services.AddLocalization();

builder.InitializeHealthChecks();

builder.InitializeGrpcClients();

builder.InitializeServices();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = HeaderConstants.XsrfToken;
});

builder.Services.AddAppRateLimiting();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.SetupRequestLoggingForBlazor();

app.SetupLocalization();

app.UseRateLimiter();

app.UseAntiforgery();

app.UseMiddleware<NotFoundLoggingMiddleware>();

app.MapLivenessHealthCheck();
app.MapHealthCheckForUptimeRobot();
app.DefineEnglishRedirectRoute();
app.MapApiEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

try
{
    Log.Information("⭐⭐ UI Starting. Can access it from: http://localhost:5002 at dev env. ⭐⭐");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Error(ex, "Unhandled exception");
}
finally
{
    await Log.CloseAndFlushAsync();
}

