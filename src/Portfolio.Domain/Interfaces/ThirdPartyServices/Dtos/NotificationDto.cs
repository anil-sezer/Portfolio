using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Interfaces.ThirdPartyServices.Dtos;

public record NotificationDto
{
    public required string Name { get; init; }
    public required string EmailAddress { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; init; }
    public required NotificationType NotificationType { get; init; }
}
