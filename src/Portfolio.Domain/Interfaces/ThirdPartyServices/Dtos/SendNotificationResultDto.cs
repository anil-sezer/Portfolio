namespace Portfolio.Domain.Interfaces.ThirdPartyServices.Dtos;

public record SendNotificationResultDto
{
    public required bool IsItSentSuccessfully { get; init; }
    public required string ErrorMessage { get; init; }
}
