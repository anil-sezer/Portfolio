using Portfolio.Ui.BackgroundServices;
using Portfolio.Ui.Services;

namespace Portfolio.Ui.Extensions;

public static class ServiceRegistration
{
    public static void InitializeServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ClusterStatsService>();
        builder.Services.AddHttpClient<BackgroundImageService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.MaxResponseContentBufferSize = 10 * 1024 * 1024;
        });
        builder.Services.AddScoped<LogVisitService>();

        builder.Services.AddHostedService<ClusterStatsWarmupService>();
        builder.Services.AddHostedService<HeroImageWarmupService>();
    }
}
