using DotNetEnv;
using Portfolio.Ui;
using Portfolio.Ui.Services;
using Portfolio.Ui.Components;
using Portfolio.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
    Env.Load("../.env");

// Trigger a get value to ensure that the env vars are loaded
_ = EnvVars.GRPC_BASE_URL;
_ = EnvVars.ASPNETCORE_ENVIRONMENT;
_ = EnvVars.OTEL_COLLECTOR_ENDPOINT;

builder.InitLogsWithSerilog();
builder.InitOpenTelemetry();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

builder.Services.AddMemoryCache();

builder.InitializeHealthChecks();

builder.InitializeGrpcClients();

// Services
builder.Services.AddSingleton<ClusterStatsService>();
builder.Services.AddSingleton<BackgroundImageService>();
builder.Services.AddSingleton<LogVisitService>();

var app = builder.Build();

// todo: check this block later. Never checked it before.
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapLivenessHealthCheck();
app.MapHealthCheckForUptimeRobot();

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// todo: Extract to its own file?
// Log 404 and 302 responses. 302 is for this: app.UseStatusCodePagesWithRedirects("/404").
app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode is 404 or 302)
    {
        Log.Information("4️⃣0️⃣4️⃣ Not Found: {RequestedUrl}", context.Request.Path.ToString());
        
        // Get LogVisitService from the service container
        var logVisitService = context.RequestServices.GetRequiredService<LogVisitService>();
        var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
        
        try
        {
            await logVisitService.LogVisitToWebpageAsync(httpContextAccessor);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to log visit for 404/302 response");
        }
    }
});

try
{
    Log.Information("UI Starting");
    app.Run();
}
catch (Exception ex)
{
    Log.Error(ex, "Unhandled exception");
}
finally
{
    await Log.CloseAndFlushAsync();
}

