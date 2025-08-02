using Microsoft.EntityFrameworkCore;

namespace Portfolio.Domain.Entities;


// todo: DB sütunlarına belli değerlerde olma zorunluluğu vs şeyleri bu katmanda ekleyebilir misin? Oku!
// https://docs.microsoft.com/en-us/ef/core/modeling/indexes?tabs=data-annotations
[Comment("Notifications that have been sent to the admin via the website ui")]
public class NotificationToAdmin: EntityBase
{
    public required string Name { get; init; }
    public required string EmailAddress { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; init; }
    public required bool IsItSentSuccessfully { get; set; }
}