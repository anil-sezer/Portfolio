namespace Portfolio.Ui.Extensions;

public static class ApiEndpointExtensions
{
    public const string EmailRoute = "/api/email/send";
    public const string LogVisitRoute = "/api/visits/log";
    
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(EmailRoute, EmailApiExtensions.SendEmail)
            .WithName("SendEmail")
            .RequireAntiforgeryToken()
            .RequireRateLimiting(RateLimiterExtensions.EmailSendPolicy);

        app.MapPost(LogVisitRoute, LogVisitApiExtensions.LogVisit)
            .WithName("LogVisit")
            .RequireAntiforgeryToken()
            .RequireRateLimiting(RateLimiterExtensions.VisitLogPolicy);

        return app;
    }
}
