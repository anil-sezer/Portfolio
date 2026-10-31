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
    public override async Task<Empty> Send(SendRequest request, ServerCallContext context)
    {
        logger.LogInformation("📧 Contact notification received for subject: {Subject}", request.Subject);
        
        var emailDto = MapToEmailDto(request);

        if (await IsThisEmailAlreadySentAtLastHourAsync(emailDto, context.CancellationToken))
        {
            logger.LogWarning("📧 Duplicate contact notification rejected within 1 hour: {Subject} by {Email}", emailDto.Subject, emailDto.EmailAddress);
            throw new RpcException(new Status(StatusCode.PermissionDenied, "This message was already sent in the last hour."));
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

    private Task<bool> IsThisEmailAlreadySentAtLastHourAsync(NotificationDto dto, CancellationToken cancellationToken = default)
    {
        return dbContext.NotificationsToAdmin
            .AnyAsync(x => x.Name == dto.Name &&
                           x.EmailAddress == dto.EmailAddress &&
                           x.Subject == dto.Subject &&
                           x.Message == dto.Message &&
                           x.CreatedAt > DateTimeOffset.UtcNow.AddHours(-1), cancellationToken);
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
