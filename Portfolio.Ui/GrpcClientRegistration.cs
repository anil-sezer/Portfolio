using Portfolio.Grpc;
using Portfolio.Infrastructure.Constants;
using Portfolio.Infrastructure.Exceptions;
using Portfolio.Infrastructure.Extensions;
using Portfolio.Infrastructure.Helpers;

namespace Portfolio.Ui;

public static class GrpcClientRegistration
{
    public static void InitializeGrpcClients(this WebApplicationBuilder builder)
    {
        builder.Services.AddGrpcClient<VisitorInsights.VisitorInsightsClient>(o =>
        {
            o.Address = new Uri(EnvVars.GRPC_BASE_URL);
        });
        builder.Services.AddGrpcClient<BackgroundImages.BackgroundImagesClient>(o =>
        {
            o.Address = new Uri(EnvVars.GRPC_BASE_URL);
        });
        builder.Services.AddGrpcClient<SendEmailToAdmin.SendEmailToAdminClient>(o =>
        {
            o.Address = new Uri(EnvVars.GRPC_BASE_URL);
        });
    }
}
