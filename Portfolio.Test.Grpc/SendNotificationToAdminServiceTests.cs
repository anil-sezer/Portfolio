using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces.ThirdPartyServices;
using Portfolio.Domain.Interfaces.ThirdPartyServices.Dtos;
using Portfolio.Grpc;
using Portfolio.Grpc.Services.SendEmailToAdmin;
using Portfolio.Infrastructure.ThirdPartyServices;
using Portfolio.Test.Shared;

namespace Portfolio.Test.Grpc;

public class SendNotificationToAdminServiceTests
{
    [Fact]
    public async Task Send_WhenDuplicateEmailSentWithinLastHour_ReturnsForbiddenAndDoesNotCallProvider()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var mockFactory = new Mock<INotificationProviderFactory>();
        var mockProvider = new Mock<INotificationProvider>();
        mockFactory.Setup(f => f.GetProvider(nameof(NotificationProviderTelegram))).Returns(mockProvider.Object);

        var service = new SendNotificationToAdminService(dbContext, mockFactory.Object);
        var context = TestServerCallContext.Create();

        // Seed an identical notification sent 10 minutes ago
        dbContext.NotificationsToAdmin.Add(new NotificationToAdmin
        {
            Name = "John",
            EmailAddress = "john@example.com",
            Subject = "Feedback",
            Message = "Great site!",
            IsItSentSuccessfully = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        await dbContext.SaveChangesAsync();

        var request = new SendRequest
        {
            SenderName = "John",
            SenderEmail = "john@example.com",
            Subject = "Feedback",
            Message = "Great site!"
        };

        // Act
        var response = await service.Send(request, context);

        // Assert
        response.ResultCode.Should().Be(ResultCode.Forbidden);
        mockProvider.Verify(p => p.SendNotificationAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Never);
        (await dbContext.NotificationsToAdmin.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Send_WhenProviderFails_StoresFailedNotificationAndReturnsError()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var mockFactory = new Mock<INotificationProviderFactory>();
        var mockProvider = new Mock<INotificationProvider>();

        mockProvider.Setup(p => p.SendNotificationAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendNotificationResultDto
            {
                IsItSentSuccessfully = false,
                ErrorMessage = "Telegram API down"
            });

        mockFactory.Setup(f => f.GetProvider(nameof(NotificationProviderTelegram))).Returns(mockProvider.Object);

        var service = new SendNotificationToAdminService(dbContext, mockFactory.Object);
        var context = TestServerCallContext.Create();

        var request = new SendRequest
        {
            SenderName = "Jane",
            SenderEmail = "jane@example.com",
            Subject = "Help",
            Message = "Need help with something."
        };

        // Act
        var response = await service.Send(request, context);

        // Assert
        response.ResultCode.Should().Be(ResultCode.Error);

        var saved = await dbContext.NotificationsToAdmin.FirstOrDefaultAsync(x => x.EmailAddress == "jane@example.com");
        saved.Should().NotBeNull();
        saved!.IsItSentSuccessfully.Should().BeFalse();
        saved.Subject.Should().Be("Help");
    }

    [Fact]
    public async Task Send_WhenProviderSucceeds_StoresSuccessfulNotificationAndReturnsSuccess()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var mockFactory = new Mock<INotificationProviderFactory>();
        var mockProvider = new Mock<INotificationProvider>();

        mockProvider.Setup(p => p.SendNotificationAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendNotificationResultDto
            {
                IsItSentSuccessfully = true,
                ErrorMessage = "Status code: OK"
            });

        mockFactory.Setup(f => f.GetProvider(nameof(NotificationProviderTelegram))).Returns(mockProvider.Object);

        var service = new SendNotificationToAdminService(dbContext, mockFactory.Object);
        var context = TestServerCallContext.Create();

        var request = new SendRequest
        {
            SenderName = "Alice",
            SenderEmail = "alice@example.com",
            Subject = "Job Offer",
            Message = "We would love to hire you!"
        };

        // Act
        var response = await service.Send(request, context);

        // Assert
        response.ResultCode.Should().Be(ResultCode.Success);

        var saved = await dbContext.NotificationsToAdmin.FirstOrDefaultAsync(x => x.EmailAddress == "alice@example.com");
        saved.Should().NotBeNull();
        saved!.IsItSentSuccessfully.Should().BeTrue();
        saved.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task Send_WhenIdenticalEmailSentMoreThanOneHourAgo_CallsProviderAndReturnsSuccess()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var mockFactory = new Mock<INotificationProviderFactory>();
        var mockProvider = new Mock<INotificationProvider>();

        mockProvider.Setup(p => p.SendNotificationAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendNotificationResultDto
            {
                IsItSentSuccessfully = true,
                ErrorMessage = "Status code: OK"
            });

        mockFactory.Setup(f => f.GetProvider(nameof(NotificationProviderTelegram))).Returns(mockProvider.Object);

        var service = new SendNotificationToAdminService(dbContext, mockFactory.Object);
        var context = TestServerCallContext.Create();

        // Seed an identical notification sent 70 minutes ago (> 1 hour)
        dbContext.NotificationsToAdmin.Add(new NotificationToAdmin
        {
            Name = "John",
            EmailAddress = "john@example.com",
            Subject = "Feedback",
            Message = "Great site!",
            IsItSentSuccessfully = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-70)
        });
        await dbContext.SaveChangesAsync();

        var request = new SendRequest
        {
            SenderName = "John",
            SenderEmail = "john@example.com",
            Subject = "Feedback",
            Message = "Great site!"
        };

        // Act
        var response = await service.Send(request, context);

        // Assert
        response.ResultCode.Should().Be(ResultCode.Success);
        mockProvider.Verify(p => p.SendNotificationAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
        (await dbContext.NotificationsToAdmin.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Send_WhenDifferentEmailSentWithinLastHour_CallsProviderAndReturnsSuccess()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var mockFactory = new Mock<INotificationProviderFactory>();
        var mockProvider = new Mock<INotificationProvider>();

        mockProvider.Setup(p => p.SendNotificationAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendNotificationResultDto
            {
                IsItSentSuccessfully = true,
                ErrorMessage = "Status code: OK"
            });

        mockFactory.Setup(f => f.GetProvider(nameof(NotificationProviderTelegram))).Returns(mockProvider.Object);

        var service = new SendNotificationToAdminService(dbContext, mockFactory.Object);
        var context = TestServerCallContext.Create();

        // Seed an email sent 5 minutes ago with DIFFERENT subject and message
        dbContext.NotificationsToAdmin.Add(new NotificationToAdmin
        {
            Name = "John",
            EmailAddress = "john@example.com",
            Subject = "Initial Question",
            Message = "How are you?",
            IsItSentSuccessfully = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        });
        await dbContext.SaveChangesAsync();

        var request = new SendRequest
        {
            SenderName = "John",
            SenderEmail = "john@example.com",
            Subject = "Follow-up",
            Message = "I have another question!"
        };

        // Act
        var response = await service.Send(request, context);

        // Assert
        response.ResultCode.Should().Be(ResultCode.Success);
        mockProvider.Verify(p => p.SendNotificationAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_WhenPreviousAttemptFailedWithinLastHour_StillEnforcesDuplicateCheck()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var mockFactory = new Mock<INotificationProviderFactory>();
        var mockProvider = new Mock<INotificationProvider>();
        mockFactory.Setup(f => f.GetProvider(nameof(NotificationProviderTelegram))).Returns(mockProvider.Object);

        var service = new SendNotificationToAdminService(dbContext, mockFactory.Object);
        var context = TestServerCallContext.Create();

        // Seed a FAILED notification attempt sent 15 minutes ago
        dbContext.NotificationsToAdmin.Add(new NotificationToAdmin
        {
            Name = "Spammer",
            EmailAddress = "spam@example.com",
            Subject = "Buy now",
            Message = "Cheap deals",
            IsItSentSuccessfully = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        });
        await dbContext.SaveChangesAsync();

        var request = new SendRequest
        {
            SenderName = "Spammer",
            SenderEmail = "spam@example.com",
            Subject = "Buy now",
            Message = "Cheap deals"
        };

        // Act
        var response = await service.Send(request, context);

        // Assert
        response.ResultCode.Should().Be(ResultCode.Forbidden);
        mockProvider.Verify(p => p.SendNotificationAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
