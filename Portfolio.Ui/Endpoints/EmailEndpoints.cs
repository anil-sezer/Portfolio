using Grpc.Core;
using Portfolio.Grpc;
using Portfolio.Ui.Extensions;
using Portfolio.Ui.Models;

namespace Portfolio.Ui.Endpoints;

public static class EmailEndpoints
{
    public const string Route = "/api/email/send";

    public static void MapEmailEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(Route, SendEmail)
            .WithName("SendEmail")
            .RequireAntiforgeryToken()
            .RequireRateLimiting(RateLimiterExtensions.EmailSendPolicy);
    }

    internal static async Task<EmailSendResponse> SendEmail(
        EmailFormModel request,
        SendEmailToAdmin.SendEmailToAdminClient emailClient,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Portfolio.Ui.Endpoints.EmailEndpoints");
        try
        {
            var grpcRequest = new SendRequest
            {
                SenderName = request.Name,
                SenderEmail = request.Email,
                Subject = request.Subject,
                Message = request.Message
            };

            await emailClient.SendAsync(grpcRequest, cancellationToken: cancellationToken);

            return new EmailSendResponse(true, EmailSendResultCode.Success);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            logger.LogWarning("Email sending rejected by rate limit: {Detail}", ex.Status.Detail);
            return new EmailSendResponse(false, EmailSendResultCode.Forbidden);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Email sending request was canceled");
            return new EmailSendResponse(false, EmailSendResultCode.Error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email");
            return new EmailSendResponse(false, EmailSendResultCode.Error);
        }
    }
}
