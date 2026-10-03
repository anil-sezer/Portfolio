using Portfolio.Ui.Models;

namespace Portfolio.Ui.Extensions;

public static class EmailApiExtensions
{

    public static async Task<EmailSendResponse> SendEmail(
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

            return new EmailSendResponse
            {
                Success = result.ResultCode == ResultCode.Success,
                Message = result.ResultMessage
            };
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Email sending request was canceled");
            return new EmailSendResponse
            {
                Success = false,
                Message = "Request was cancelled."
            };
        }
        catch (Exception ex)
        {
            Log.Error("Failed to send email: {ExceptionMessage}", ex.Message);
            return new EmailSendResponse
            {
                Success = false,
                Message = "An error occurred while sending your email. Please try again later."
            };
        }
    }
}

public class EmailSendResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
}
