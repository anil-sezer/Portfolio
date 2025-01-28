using DotNetEnv;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Portfolio.Infrastructure.Constants;
using Portfolio.Grpc;
using Portfolio.Infrastructure.Exceptions;
using Portfolio.Infrastructure.Extensions;
using Portfolio.Ui.Components;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    Env.Load("../.env");

EnvironmentExtensions.VerifyEnvironmentValuesAreSet([
    EnvironmentVariableNames.Grpc_BaseUrl, 
    EnvironmentVariableNames.OpenTelemetry_CollectorEndpoint,
    EnvironmentVariableNames.DevOrProd
]);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.InitLogsWithSerilog();
builder.InitOpenTelemetry();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

var grpcAddress = Environment.GetEnvironmentVariable(EnvironmentVariableNames.Grpc_BaseUrl) ?? throw new MissingEnvironmentValueException();
builder.Services.AddGrpcClient<VisitorInsights.VisitorInsightsClient>(o =>
{
    o.Address = new Uri(grpcAddress);
});
builder.Services.AddGrpcClient<BackgroundImages.BackgroundImagesClient>(o =>
{
    o.Address = new Uri(grpcAddress);
});
builder.Services.AddGrpcClient<SendEmailToAdmin.SendEmailToAdminClient>(o =>
{
    o.Address = new Uri(grpcAddress);
});

var app = builder.Build();

// todo: check this block later. Never checked it before.
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapHealthChecks(DefaultValues.HealthCheck_Liveness, new HealthCheckOptions
{
    Predicate = _ => false, // Always return healthy for liveness
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK, // Liveness doesn't degrade
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapMethods(DefaultValues.HealthCheck_ThirdParty, [HttpMethods.Head], () =>
{
    return Results.Ok();
});

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

