using System.Text;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces.ThirdPartyServices;
using Portfolio.Domain.Interfaces.ThirdPartyServices.Dtos;
using Portfolio.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace Portfolio.Infrastructure.ThirdPartyServices;

public class NotificationProviderTelegram(HttpClient httpClient, ILogger<NotificationProviderTelegram> logger) : INotificationProvider
{
    public async Task<SendNotificationResultDto> SendNotificationAsync(NotificationDto dto, CancellationToken cancellationToken = default)
    {
        var telegramDto = dto with { Message = AdaptMessageForTelegram(dto) };
        
        var url = $"https://api.telegram.org/bot{EnvVars.NOTIFICATION_TELEGRAM_API_KEY}/sendMessage";
        var payload = new Dictionary<string, string>
        {
            { "chat_id", EnvVars.NOTIFICATION_TELEGRAM_CHAT_ID },
            { "text", FormatTelegramMessage(telegramDto) },
            { "parse_mode", "MarkdownV2" }
        };

        var content = new FormUrlEncodedContent(payload);
        try
        {
            var response = await httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return new SendNotificationResultDto
            {
                IsItSentSuccessfully = true,
                ErrorMessage = $"Status code: {response.StatusCode}"
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send Telegram notification: {Message}", e.Message);
            
            return new SendNotificationResultDto
            {
                IsItSentSuccessfully = false,
                ErrorMessage = e.Message
            };
        }
    }
    
    private static string FormatTelegramMessage(NotificationDto dto)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        string emoji, prefix;

        switch (dto.NotificationType)
        {
            case NotificationType.Info:
                emoji = "ℹ️";
                prefix = "*[INFO]*";
                break;
            case NotificationType.Success:
                emoji = "✅";
                prefix = "*[SUCCESS]*";
                break;
            case NotificationType.Error:
                emoji = "❌";
                prefix = "*[ERROR]*";
                break;
            case NotificationType.Warning:
                emoji = "⚠️";
                prefix = "*[WARNING]*";
                break;
            case NotificationType.Fatal:
                emoji = "🚨🚨🚨";
                prefix = "*[FATAL]*";
                break;
            case NotificationType.Unknown:
            default:
                emoji = "📌";
                prefix = "*[LOG]*";
                break;
        }

        var safeMessage = EscapeMarkdown(dto.Message);
        return $"{emoji} {prefix} `{timestamp}`\n{safeMessage}";
    }

    private static string EscapeMarkdown(string input)
    {
        var charsToEscape = new[] { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
        foreach (var c in charsToEscape)
        {
            input = input.Replace(c, $"\\{c}");
        }
        return input;
    }
    
    private static string AdaptMessageForTelegram(NotificationDto dto)
    {
        var sb = new StringBuilder();
        sb.AppendLine("✉️ *NEW EMAIL* ✉️");
        sb.AppendLine($"*Sender Name:* {dto.Name}");
        sb.AppendLine($"*Sender Email:* {dto.EmailAddress}");
        sb.AppendLine($"*Subject:* {dto.Subject}");
        sb.AppendLine();
        sb.AppendLine("*Message:*");
        sb.AppendLine(dto.Message);

        return sb.ToString();
    }
}