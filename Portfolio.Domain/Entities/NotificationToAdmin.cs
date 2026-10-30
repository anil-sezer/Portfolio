using Portfolio.Domain.Extensions;

namespace Portfolio.Domain.Entities;

public class NotificationToAdmin : EntityBase
{
    public const int MaxNameLength = 100;
    public const int MaxEmailAddressLength = 256;
    public const int MaxSubjectLength = 200;
    public const int MaxMessageLength = 4000;

    private readonly string _name = string.Empty;
    public required string Name
    {
        get => _name;
        init => _name = value.Truncate(MaxNameLength);
    }

    private readonly string _emailAddress = string.Empty;
    public required string EmailAddress
    {
        get => _emailAddress;
        init => _emailAddress = value.Truncate(MaxEmailAddressLength);
    }

    private readonly string _subject = string.Empty;
    public required string Subject
    {
        get => _subject;
        init => _subject = value.Truncate(MaxSubjectLength);
    }

    private readonly string _message = string.Empty;
    public required string Message
    {
        get => _message;
        init => _message = value.Truncate(MaxMessageLength);
    }

    public required bool IsItSentSuccessfully { get; set; }
}