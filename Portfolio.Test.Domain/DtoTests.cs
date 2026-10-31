using FluentAssertions;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces.ThirdPartyServices.Dtos;

namespace Portfolio.Test.Domain;

public class DtoTests
{
    [Fact]
    public void NotificationDto_ShouldHaveRecordValueEquality()
    {
        var dto1 = new NotificationDto
        {
            Name = "John Doe",
            EmailAddress = "john@example.com",
            Subject = "Test Subject",
            Message = "Test Message",
            NotificationType = NotificationType.Info
        };

        var dto2 = new NotificationDto
        {
            Name = "John Doe",
            EmailAddress = "john@example.com",
            Subject = "Test Subject",
            Message = "Test Message",
            NotificationType = NotificationType.Info
        };

        dto1.Should().Be(dto2);
        (dto1 == dto2).Should().BeTrue();
    }

    [Fact]
    public void NotificationDto_ShouldSupportNonDestructiveMutationViaWith()
    {
        var original = new NotificationDto
        {
            Name = "Original Name",
            EmailAddress = "original@example.com",
            Subject = "Original Subject",
            Message = "Original Message",
            NotificationType = NotificationType.Info
        };

        var modified = original with { Name = "Updated Name", NotificationType = NotificationType.Error };

        modified.Name.Should().Be("Updated Name");
        modified.NotificationType.Should().Be(NotificationType.Error);
        modified.EmailAddress.Should().Be(original.EmailAddress);
        original.Name.Should().Be("Original Name");
    }

    [Fact]
    public void SendNotificationResultDto_ShouldHaveRecordValueEquality()
    {
        var result1 = new SendNotificationResultDto
        {
            IsItSentSuccessfully = true,
            ErrorMessage = string.Empty
        };

        var result2 = new SendNotificationResultDto
        {
            IsItSentSuccessfully = true,
            ErrorMessage = string.Empty
        };

        result1.Should().Be(result2);
        (result1 == result2).Should().BeTrue();
    }

    [Fact]
    public void SendNotificationResultDto_ShouldSupportNonDestructiveMutationViaWith()
    {
        var original = new SendNotificationResultDto
        {
            IsItSentSuccessfully = false,
            ErrorMessage = "Connection timeout"
        };

        var modified = original with { IsItSentSuccessfully = true, ErrorMessage = string.Empty };

        modified.IsItSentSuccessfully.Should().BeTrue();
        modified.ErrorMessage.Should().BeEmpty();
        original.IsItSentSuccessfully.Should().BeFalse();
        original.ErrorMessage.Should().Be("Connection timeout");
    }
}
