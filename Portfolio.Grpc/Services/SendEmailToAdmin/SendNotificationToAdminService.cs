using System.Text.Json;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces.Repositories;
using Portfolio.Domain.Interfaces.Repositories.Dtos;
using Portfolio.Domain.Interfaces.ThirdPartyServices;
using Portfolio.Domain.Interfaces.ThirdPartyServices.Dtos;
using Portfolio.Infrastructure.ThirdPartyServices;

namespace Portfolio.Grpc.Services.SendEmailToAdmin;

public class SendNotificationToAdminService(INotificationToAdminRepository notificationToAdminRepo, INotificationProviderFactory notificationProviderFactory): Grpc.SendEmailToAdmin.SendEmailToAdminBase
{
    // todo: I wanna use MediatR here
    public override async Task<SendResponse> Send(SendRequest r, ServerCallContext context)
    {
        Log.Information("📧 Request to log: {Log}", JsonSerializer.Serialize(r));
        
        var emailDto = MapToEmailDto(r);

        if (await notificationToAdminRepo.IsThisEmailAlreadySentAtLastHourAsync(emailDto))
        {
            return new SendResponse
            {
                ResultCode = ResultCode.Forbidden,
                ResultMessage = "You already sent this mail. Lets try other methods to reach me eh?"
            };
        }
        
        var result = await SendNotificationToAdminAsync(emailDto);
        if (result.IsItSentSuccessfully == false)
        {
            Log.Error("📧 ❌ Failed to send email. Error: {Error}", result.ErrorMessage);
            await StoreNotificationAtDb(emailDto, false);
            return new SendResponse
            {
                ResultCode = ResultCode.Error,
                ResultMessage = DefaultValues.SendEmail_ErrorMessage
            };
        }

        await StoreNotificationAtDb(emailDto, true);
        return new SendResponse
        {
            ResultCode = ResultCode.Success,
            ResultMessage = "Email sent successfully! I will read it soon as I can, thanks!"
        };
    }

    private static NotificationDto MapToEmailDto(SendRequest r)
    {
        return new NotificationDto
        {
            Name = r.SenderName,
            EmailAddress = r.SenderEmail,
            Subject = r.Subject,
            Message = r.Message,
            NotificationType = NotificationType.Info
        };
    }

    private async Task<SendNotificationResultDto> SendNotificationToAdminAsync(NotificationDto dto)
    {
        var emailProvider = notificationProviderFactory.GetProvider(nameof(NotificationProviderTelegram));
        return await emailProvider.SendNotificationAsync(dto);
    }

    private async Task StoreNotificationAtDb(NotificationDto dto, bool isItSentSuccessfully)
    {
        await notificationToAdminRepo.CreateAsync(new NotificationToAdmin
        {
            Name = dto.Name,
            EmailAddress = dto.EmailAddress,
            Subject = dto.Subject,
            Message = dto.Message,
            IsItSentSuccessfully = isItSentSuccessfully
        });
    }
}
