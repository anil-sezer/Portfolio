using Portfolio.Grpc;
using Portfolio.Infrastructure.Constants;
using Portfolio.Infrastructure.Exceptions;
using Portfolio.Infrastructure.Helpers;

namespace Portfolio.Ui;

public static class GrpcClientRegistration
{
    public static void InitializeGrpcClients(this WebApplicationBuilder builder)
    {
        var grpcAddress = EnvVarHelpers.GetValue(EnvVarNames.Grpc_BaseUrl) ?? throw new MissingEnvironmentValueException();
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
    }
}
