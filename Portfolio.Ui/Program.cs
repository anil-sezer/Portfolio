using System.Globalization;
using DotNetEnv;
using Microsoft.AspNetCore.Localization;
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
builder.InitOpenTelemetry("Portfolio.Ui" + (builder.Environment.IsDevelopment() ? ".Dev" : ""));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

builder.Services.AddMemoryCache();

// builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddLocalization();

builder.InitializeHealthChecks();

builder.InitializeGrpcClients();

// Services
builder.Services.AddSingleton<ClusterStatsService>();
builder.Services.AddSingleton<BackgroundImageService>();
builder.Services.AddSingleton<LogVisitService>();

var app = builder.Build();

var supportedCultures = new[]
{
    new CultureInfo("en-US"),
    new CultureInfo("en"),
    new CultureInfo("tr-TR"),
    new CultureInfo("tr")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

app.UseRequestLocalization(localizationOptions);

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

app.MapGet("/SetCulture", (string culture, string? redirectUri, HttpContext httpContext) =>
{
    if (!string.IsNullOrWhiteSpace(culture))
    {
        httpContext.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax }
        );
    }
    return Results.LocalRedirect(string.IsNullOrWhiteSpace(redirectUri) ? "/" : redirectUri);
});

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

