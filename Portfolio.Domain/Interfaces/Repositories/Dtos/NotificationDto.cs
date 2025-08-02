using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Interfaces.Repositories.Dtos;

public class NotificationDto
{
    public required string Name { get; init; }
    public required string EmailAddress { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; set; }
    public required NotificationType NotificationType { get; init; }
}