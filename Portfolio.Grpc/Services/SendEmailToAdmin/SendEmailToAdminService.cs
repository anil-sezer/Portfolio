using System.Text.Json;
using Portfolio.Domain.Interfaces.Repositories;
using Portfolio.Domain.Interfaces.Repositories.Dtos;
using Portfolio.Domain.Interfaces.ThirdPartyServices;
using Portfolio.Domain.Interfaces.ThirdPartyServices.Dtos;
using Portfolio.Infrastructure.ThirdPartyServices;

namespace Portfolio.Grpc.Services.SendEmailToAdmin;

public class SendEmailToAdminService(IEmailToAdminRepository emailToAdminRepository, IEmailProviderFactory emailProviderFactory): Grpc.SendEmailToAdmin.SendEmailToAdminBase
{
    // todo: I wanna use MediatR here
    public override async Task<SendResponse> Send(SendRequest r, ServerCallContext context)
    {
        Log.Information("📧 Request to log: {Log}", JsonSerializer.Serialize(r));
        
        var emailDto = MapToEmailDto(r);

        if (await emailToAdminRepository.IsThisEmailAlreadySentAtLastHourAsync(emailDto))
        {
            return new SendResponse
            {
                ResultCode = ResultCode.Forbidden,
                ResultMessage = "You already sent this mail. Lets try other methods to reach me eh?"
            };
        }
        
        var result = await SendEmailToAdminAsync(emailDto);
        if (result.IsItSentSuccessfully == false)
        {
            Log.Error("📧 ❌ Failed to send email. Error: {Error}", result.ErrorMessage);
            await StoreEmailAtDb(emailDto, false);
            return new SendResponse
            {
                ResultCode = ResultCode.Error,
                ResultMessage = DefaultValues.SendEmail_ErrorMessage
            };
        }

        await StoreEmailAtDb(emailDto, true);
        return new SendResponse
        {
            ResultCode = ResultCode.Success,
            ResultMessage = "Email sent successfully! I will read it soon as I can, thanks!"
        };
    }

    private static EmailDto MapToEmailDto(SendRequest r)
    {
        return new EmailDto
        {
            Name = r.SenderName,
            EmailAddress = r.SenderEmail,
            Subject = r.Subject,
            Message = r.Message
        };
    }

    private async Task<SendEmailResultDto> SendEmailToAdminAsync(EmailDto dto)
    {
        var emailProvider = emailProviderFactory.GetProvider(nameof(SlackEmailProvider));
        return await emailProvider.SendEmailAsync(dto);
    }

    private async Task StoreEmailAtDb(EmailDto dto, bool isItSentSuccessfully)
    {
        await emailToAdminRepository.CreateAsync(new EmailToAdmin
        {
            Name = dto.Name,
            EmailAddress = dto.EmailAddress,
            Subject = dto.Subject,
            Message = dto.Message,
            IsItSentSuccessfully = isItSentSuccessfully
        });
    }
}
