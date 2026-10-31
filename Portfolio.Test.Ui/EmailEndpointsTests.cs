using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portfolio.Grpc;
using Portfolio.Test.Shared;
using Portfolio.Ui.Endpoints;
using Portfolio.Ui.Models;

namespace Portfolio.Test.Ui;

public class EmailEndpointsTests
{
    private static readonly EmailFormModel ValidModel = new()
    {
        Name = "Alice",
        Email = "alice@example.com",
        Subject = "Hello",
        Message = "Test message content"
    };

    [Fact]
    public async Task SendEmail_WhenClientSucceeds_ReturnsSuccessResponse()
    {
        // Arrange
        var mockClient = new Mock<SendEmailToAdmin.SendEmailToAdminClient>();
        mockClient.Setup(c => c.SendAsync(
                It.IsAny<SendRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(new Empty()));

        // Act
        var result = await EmailEndpoints.SendEmail(
            ValidModel,
            mockClient.Object,
            NullLoggerFactory.Instance,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ResultCode.Should().Be(EmailSendResultCode.Success);
    }

    [Fact]
    public async Task SendEmail_WhenRateLimitedByGrpc_ReturnsForbiddenResponse()
    {
        // Arrange
        var mockClient = new Mock<SendEmailToAdmin.SendEmailToAdminClient>();
        var rpcException = new RpcException(new Status(StatusCode.PermissionDenied, "Rate limited"));
        mockClient.Setup(c => c.SendAsync(
                It.IsAny<SendRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(Task.FromException<Empty>(rpcException)));

        // Act
        var result = await EmailEndpoints.SendEmail(
            ValidModel,
            mockClient.Object,
            NullLoggerFactory.Instance,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ResultCode.Should().Be(EmailSendResultCode.Forbidden);
    }

    [Fact]
    public async Task SendEmail_WhenGrpcFailsWithInternalError_ReturnsErrorResponse()
    {
        // Arrange
        var mockClient = new Mock<SendEmailToAdmin.SendEmailToAdminClient>();
        var rpcException = new RpcException(new Status(StatusCode.Internal, "Telegram failed"));
        mockClient.Setup(c => c.SendAsync(
                It.IsAny<SendRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(Task.FromException<Empty>(rpcException)));

        // Act
        var result = await EmailEndpoints.SendEmail(
            ValidModel,
            mockClient.Object,
            NullLoggerFactory.Instance,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ResultCode.Should().Be(EmailSendResultCode.Error);
    }

    [Fact]
    public async Task SendEmail_WhenOperationCanceled_ReturnsErrorResponse()
    {
        // Arrange
        var mockClient = new Mock<SendEmailToAdmin.SendEmailToAdminClient>();
        mockClient.Setup(c => c.SendAsync(
                It.IsAny<SendRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(Task.FromCanceled<Empty>(new CancellationToken(true))));

        // Act
        var result = await EmailEndpoints.SendEmail(
            ValidModel,
            mockClient.Object,
            NullLoggerFactory.Instance,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ResultCode.Should().Be(EmailSendResultCode.Error);
    }
}
