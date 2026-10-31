using FluentAssertions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
    public async Task Send_WhenDuplicateEmailSentWithinLastHour_ThrowsPermissionDeniedAndDoesNotCallProvider()
    {
        // Arrange
        await using var dbContext = TestDbContextFactory.Create();
        var mockFactory = new Mock<INotificationProviderFactory>();
        var mockProvider = new Mock<INotificationProvider>();
        mockFactory.Setup(f => f.GetProvider(nameof(NotificationProviderTelegram))).Returns(mockProvider.Object);

        var service = new SendNotificationToAdminService(dbContext, mockFactory.Object, NullLogger<SendNotificationToAdminService>.Instance);
        var context = TestServerCallContext.Create();

        // Seed an identical notification sent 10 minutes ago
        dbContext.NotificationsToAdmin.Add(new NotificationToAdmin
        {
            Name = "John",
            EmailAddress = "john@example.com",
            Subject = "Feedback",
            Message = "Great site!",
            IsItSentSuccessfully = true,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
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
        var act = () => service.Send(request, context);

        // Assert
        var thrown = await act.Should().ThrowAsync<RpcException>();
        thrown.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
        thrown.Which.Status.Detail.Should().Contain("already sent");
        mockProvider.Verify(p => p.SendNotificationAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Never);
        (await dbContext.NotificationsToAdmin.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Send_WhenProviderFails_StoresFailedNotificationAndThrowsInternalRpcException()
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

        var service = new SendNotificationToAdminService(dbContext, mockFactory.Object, NullLogger<SendNotificationToAdminService>.Instance);
        var context = TestServerCallContext.Create();

        var request = new SendRequest
        {
            SenderName = "Jane",
            SenderEmail = "jane@example.com",
            Subject = "Help",
            Message = "Need help with something."
        };

        // Act
        var act = () => service.Send(request, context);

        // Assert
        var thrown = await act.Should().ThrowAsync<RpcException>();
        thrown.Which.StatusCode.Should().Be(StatusCode.Internal);
        thrown.Which.Status.Detail.Should().Contain("Failed to send notification");

        var saved = await dbContext.NotificationsToAdmin.FirstOrDefaultAsync(x => x.EmailAddress == "jane@example.com");
        saved.Should().NotBeNull();
        saved!.IsItSentSuccessfully.Should().BeFalse();
        saved.Subject.Should().Be("Help");
    }

    [Fact]
    public async Task Send_WhenProviderSucceeds_StoresSuccessfulNotificationAndReturnsEmpty()
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

        var service = new SendNotificationToAdminService(dbContext, mockFactory.Object, NullLogger<SendNotificationToAdminService>.Instance);
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
        response.Should().NotBeNull();

        var saved = await dbContext.NotificationsToAdmin.FirstOrDefaultAsync(x => x.EmailAddress == "alice@example.com");
        saved.Should().NotBeNull();
        saved!.IsItSentSuccessfully.Should().BeTrue();
        saved.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task Send_WhenIdenticalEmailSentMoreThanOneHourAgo_CallsProviderAndReturnsEmpty()
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

        var service = new SendNotificationToAdminService(dbContext, mockFactory.Object, NullLogger<SendNotificationToAdminService>.Instance);
        var context = TestServerCallContext.Create();

        // Seed an identical notification sent 70 minutes ago (> 1 hour)
        dbContext.NotificationsToAdmin.Add(new NotificationToAdmin
        {
            Name = "John",
            EmailAddress = "john@example.com",
            Subject = "Feedback",
            Message = "Great site!",
            IsItSentSuccessfully = true,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-70)
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
        response.Should().NotBeNull();
        mockProvider.Verify(p => p.SendNotificationAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
        (await dbContext.NotificationsToAdmin.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Send_WhenDifferentEmailSentWithinLastHour_CallsProviderAndReturnsEmpty()
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

        var service = new SendNotificationToAdminService(dbContext, mockFactory.Object, NullLogger<SendNotificationToAdminService>.Instance);
        var context = TestServerCallContext.Create();

        // Seed an email sent 5 minutes ago with DIFFERENT subject and message
        dbContext.NotificationsToAdmin.Add(new NotificationToAdmin
        {
            Name = "John",
            EmailAddress = "john@example.com",
            Subject = "Initial Question",
            Message = "How are you?",
            IsItSentSuccessfully = true,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
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
        response.Should().NotBeNull();
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

        var service = new SendNotificationToAdminService(dbContext, mockFactory.Object, NullLogger<SendNotificationToAdminService>.Instance);
        var context = TestServerCallContext.Create();

        // Seed a FAILED notification attempt sent 15 minutes ago
        dbContext.NotificationsToAdmin.Add(new NotificationToAdmin
        {
            Name = "Spammer",
            EmailAddress = "spam@example.com",
            Subject = "Buy now",
            Message = "Cheap deals",
            IsItSentSuccessfully = false,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-15)
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
        var act = () => service.Send(request, context);

        // Assert
        var thrown = await act.Should().ThrowAsync<RpcException>();
        thrown.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
        mockProvider.Verify(p => p.SendNotificationAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
