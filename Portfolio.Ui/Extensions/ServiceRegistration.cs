using Portfolio.Ui.Services;

namespace Portfolio.Ui.Extensions;

public static class ServiceRegistration
{
    public static void InitializeServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ClusterStatsService>();
        builder.Services.AddScoped<BackgroundImageService>();
        builder.Services.AddScoped<LogVisitService>();
    }
}
