namespace Portfolio.Domain.Interfaces.ThirdPartyServices.Dtos;

public class SendNotificationResultDto
{
    public required bool IsItSentSuccessfully { get; init; }
    public required string ErrorMessage { get; init; }
}