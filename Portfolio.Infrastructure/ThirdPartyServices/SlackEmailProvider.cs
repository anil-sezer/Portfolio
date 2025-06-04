using System.Text;
using System.Text.Json;
using Portfolio.Domain.Interfaces.Repositories.Dtos;
using Portfolio.Domain.Interfaces.ThirdPartyServices;
using Portfolio.Domain.Interfaces.ThirdPartyServices.Dtos;
using Portfolio.Infrastructure.Constants;
using Portfolio.Infrastructure.Helpers;
using Serilog;

namespace Portfolio.Infrastructure.ThirdPartyServices;

public class SlackEmailProvider : IEmailProvider
{
    public async Task<SendEmailResultDto> SendEmailAsync(EmailDto dto)
    {
        using HttpClient client = new();
        var payload = new { text = AdaptMessageForSlack(dto) };
        
        var jsonPayload = JsonSerializer.Serialize(payload);

        var webhookUrl = EnvVarHelpers.GetValue(EnvVarNames.Email_Slack_WebhookUrl);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(webhookUrl, content);

        if (response.IsSuccessStatusCode)
        {
            Log.Information("📧 ✅ Email sent successfully!");
            return new SendEmailResultDto
            {
                IsItSentSuccessfully = true,
                ErrorMessage = $"Status code: {response.StatusCode}"
            };
        }

        var msg = $"📧 ❌ Failed to send message. Status: {response.StatusCode}";
        Log.Error(msg);
        
        return new SendEmailResultDto
        {
            IsItSentSuccessfully = false,
            ErrorMessage = msg
        };
    }

    private static string AdaptMessageForSlack(EmailDto dto)
    {
        var sb = new StringBuilder();
        sb.AppendLine(":envelope: *NEW EMAIL* :envelope:");
        sb.AppendLine($"*Sender Name:* {dto.Name}");
        sb.AppendLine($"*Sender Email:* {dto.EmailAddress}");
        sb.AppendLine($"*Subject:* {dto.Subject}");
        sb.AppendLine();
        sb.AppendLine("*Message:*");
        sb.AppendLine(dto.Message);

        return sb.ToString();
    }
}