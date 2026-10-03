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

    private static async Task<EmailSendResponse> SendEmail(
        EmailFormModel request,
        SendEmailToAdmin.SendEmailToAdminClient emailClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var grpcRequest = new SendRequest
            {
                SenderName = request.Name,
                SenderEmail = request.Email,
                Subject = request.Subject,
                Message = request.Message
            };

            var result = await emailClient.SendAsync(grpcRequest, cancellationToken: cancellationToken);

            return new EmailSendResponse(
                result.ResultCode == ResultCode.Success,
                result.ResultMessage);
        }
        catch (OperationCanceledException ex)
        {
            Log.Warning(ex, "Email sending request was canceled");
            return new EmailSendResponse(false, "Request was cancelled.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send email");
            return new EmailSendResponse(
                false,
                "An error occurred while sending your email. Please try again later.");
        }
    }
}
