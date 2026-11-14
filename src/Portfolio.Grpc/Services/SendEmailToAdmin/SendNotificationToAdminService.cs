using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Extensions;
using Portfolio.Domain.Interfaces.ThirdPartyServices;
using Portfolio.Domain.Interfaces.ThirdPartyServices.Dtos;
using Portfolio.Infrastructure;
using Portfolio.Infrastructure.ThirdPartyServices;

namespace Portfolio.Grpc.Services.SendEmailToAdmin;

public class SendNotificationToAdminService(
    PortfolioDbContext dbContext,
    INotificationProviderFactory notificationProviderFactory,
    ILogger<SendNotificationToAdminService> logger) : Grpc.SendEmailToAdmin.SendEmailToAdminBase
{
    public const int MaxEmailsPerSenderPerHour = 3;
    public const int MaxGlobalEmailsPerHour = 20;

    public override async Task<Empty> Send(SendRequest request, ServerCallContext context)
    {
        logger.LogInformation("📧 Contact notification received for subject: {Subject}", request.Subject);

        var emailDto = MapToEmailDto(request);
        var oneHourAgo = DateTimeOffset.UtcNow.AddHours(-1);

        if (await IsThisEmailAlreadySentAtLastHourAsync(emailDto, oneHourAgo, context.CancellationToken))
        {
            logger.LogWarning("📧 Duplicate contact notification rejected within 1 hour: {Subject} by {Email}", emailDto.Subject, emailDto.EmailAddress);
            throw new RpcException(new Status(StatusCode.PermissionDenied, "This message was already sent in the last hour."));
        }

        if (await IsSenderThrottledAsync(emailDto.EmailAddress, oneHourAgo, context.CancellationToken))
        {
            logger.LogWarning("📧 Sender hourly limit ({Limit}) reached for {Email}", MaxEmailsPerSenderPerHour, emailDto.EmailAddress);
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Too many messages sent from this email address in the last hour."));
        }

        if (await IsGlobalFloodLimitReachedAsync(oneHourAgo, context.CancellationToken))
        {
            logger.LogWarning("📧 Global hourly contact notification limit ({Limit}) reached; rejecting {Email}", MaxGlobalEmailsPerHour, emailDto.EmailAddress);
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Contact form rate limit exceeded. Please try again later."));
        }
        
        var result = await SendNotificationToAdminAsync(emailDto, context.CancellationToken);
        if (!result.IsItSentSuccessfully)
        {
            logger.LogError("📧 ❌ Failed to send email. Error: {Error}", result.ErrorMessage);
            await StoreNotificationAtDb(emailDto, false, context.CancellationToken);
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to send notification: {result.ErrorMessage}"));
        }

        await StoreNotificationAtDb(emailDto, true, context.CancellationToken);
        return new Empty();
    }

    private static NotificationDto MapToEmailDto(SendRequest r)
    {
        return new NotificationDto
        {
            Name = r.SenderName.Truncate(NotificationToAdmin.MaxNameLength),
            EmailAddress = r.SenderEmail.Truncate(NotificationToAdmin.MaxEmailAddressLength),
            Subject = r.Subject.Truncate(NotificationToAdmin.MaxSubjectLength),
            Message = r.Message.Truncate(NotificationToAdmin.MaxMessageLength),
            NotificationType = NotificationType.Info
        };
    }

    private async Task<SendNotificationResultDto> SendNotificationToAdminAsync(NotificationDto dto, CancellationToken cancellationToken = default)
    {
        var emailProvider = notificationProviderFactory.GetProvider(nameof(NotificationProviderTelegram));
        return await emailProvider.SendNotificationAsync(dto, cancellationToken);
    }

    private Task<bool> IsThisEmailAlreadySentAtLastHourAsync(NotificationDto dto, DateTimeOffset oneHourAgo, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = dto.EmailAddress.Trim().ToLowerInvariant();
        return dbContext.NotificationsToAdmin
            .AnyAsync(x => x.Name == dto.Name &&
                           x.EmailAddress.ToLower() == normalizedEmail &&
                           x.Subject == dto.Subject &&
                           x.Message == dto.Message &&
                           x.CreatedAt > oneHourAgo, cancellationToken);
    }

    private async Task<bool> IsSenderThrottledAsync(string emailAddress, DateTimeOffset oneHourAgo, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = emailAddress.Trim().ToLowerInvariant();
        var recentCount = await dbContext.NotificationsToAdmin
            .CountAsync(x => x.CreatedAt > oneHourAgo && x.EmailAddress.ToLower() == normalizedEmail, cancellationToken);

        return recentCount >= MaxEmailsPerSenderPerHour;
    }

    private async Task<bool> IsGlobalFloodLimitReachedAsync(DateTimeOffset oneHourAgo, CancellationToken cancellationToken = default)
    {
        var recentCount = await dbContext.NotificationsToAdmin
            .CountAsync(x => x.CreatedAt > oneHourAgo, cancellationToken);

        return recentCount >= MaxGlobalEmailsPerHour;
    }

    private async Task StoreNotificationAtDb(NotificationDto dto, bool isItSentSuccessfully, CancellationToken cancellationToken = default)
    {
        dbContext.NotificationsToAdmin.Add(new NotificationToAdmin
        {
            Name = dto.Name,
            EmailAddress = dto.EmailAddress,
            Subject = dto.Subject,
            Message = dto.Message,
            IsItSentSuccessfully = isItSentSuccessfully
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
