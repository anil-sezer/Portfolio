using DotNetEnv;
using Portfolio.Infrastructure.Constants;
using Portfolio.Infrastructure.Extensions;
using Portfolio.Infrastructure.Helpers;
using Portfolio.Ui;
using Portfolio.Ui.Components;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.InitLogsWithSerilog();
builder.InitOpenTelemetry();

EnvVarHelpers.VerifyEnvironmentValuesAreSet([
    EnvVarNames.Grpc_BaseUrl, 
    EnvVarNames.OpenTelemetry_CollectorEndpoint,
    EnvVarNames.DevOrProd
]);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

builder.InitializeHealthChecks();

builder.InitializeGrpcClients();

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

// Log 404 and 302 responses. 302 is for this: app.UseStatusCodePagesWithRedirects("/404").
app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode is 404 or 302)
        Log.Information("4️⃣0️⃣4️⃣ Not Found: {RequestedUrl}", context.Request.Path.ToString());
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

