using System.Buffers;
using System.Text;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces.ThirdPartyServices;
using Portfolio.Domain.Interfaces.ThirdPartyServices.Dtos;
using Portfolio.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace Portfolio.Infrastructure.ThirdPartyServices;

public class NotificationProviderTelegram(HttpClient httpClient, ILogger<NotificationProviderTelegram> logger) : INotificationProvider
{
    private static readonly SearchValues<char> MarkdownV2SpecialChars =
        SearchValues.Create(@"\_*[]()~`>#+-=|{}.!");

    public async Task<SendNotificationResultDto> SendNotificationAsync(NotificationDto dto, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.telegram.org/bot{EnvVars.NOTIFICATION_TELEGRAM_API_KEY}/sendMessage";
        var payload = new Dictionary<string, string>
        {
            { "chat_id", EnvVars.NOTIFICATION_TELEGRAM_CHAT_ID },
            { "text", FormatTelegramMessage(dto) },
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
        var (emoji, prefix) = dto.NotificationType switch
        {
            NotificationType.Info => ("ℹ️", @"*\[INFO\]*"),
            NotificationType.Success => ("✅", @"*\[SUCCESS\]*"),
            NotificationType.Error => ("❌", @"*\[ERROR\]*"),
            NotificationType.Warning => ("⚠️", @"*\[WARNING\]*"),
            NotificationType.Fatal => ("🚨🚨🚨", @"*\[FATAL\]*"),
            _ => ("📌", @"*\[LOG\]*")
        };

        return $"{emoji} {prefix} `{timestamp}`\n{AdaptMessageForTelegram(dto)}";
    }

    private static string EscapeMarkdown(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var firstSpecialIndex = input.AsSpan().IndexOfAny(MarkdownV2SpecialChars);
        if (firstSpecialIndex < 0)
        {
            return input;
        }

        var sb = new StringBuilder(input.Length + 8);
        sb.Append(input, 0, firstSpecialIndex);
        for (var i = firstSpecialIndex; i < input.Length; i++)
        {
            var c = input[i];
            if (MarkdownV2SpecialChars.Contains(c))
            {
                sb.Append('\\');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static string AdaptMessageForTelegram(NotificationDto dto)
    {
        var sb = new StringBuilder();
        sb.AppendLine("✉️ *NEW EMAIL* ✉️");
        sb.AppendLine($"*Sender Name:* {EscapeMarkdown(dto.Name)}");
        sb.AppendLine($"*Sender Email:* {EscapeMarkdown(dto.EmailAddress)}");
        sb.AppendLine($"*Subject:* {EscapeMarkdown(dto.Subject)}");
        sb.AppendLine();
        sb.AppendLine("*Message:*");
        sb.AppendLine(EscapeMarkdown(dto.Message));

        return sb.ToString();
    }
}