using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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

    [Theory]
    [MemberData(nameof(InvalidEmailPayloads))]
    public async Task PostEmailEndpoint_WithInvalidModel_ReturnsBadRequestAndSkipsGrpc(
        object payload,
        string expectedErrorField)
    {
        // Arrange
        var mockClient = new Mock<SendEmailToAdmin.SendEmailToAdminClient>();
        await using var app = await CreateValidationTestAppAsync(mockClient.Object);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

        // Act
        var response = await client.PostAsJsonAsync(EmailEndpoints.Route, payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey(expectedErrorField);

        mockClient.Verify(c => c.SendAsync(
            It.IsAny<SendRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PostEmailEndpoint_WithValidModel_ReturnsOkAndCallsGrpc()
    {
        // Arrange
        var mockClient = new Mock<SendEmailToAdmin.SendEmailToAdminClient>();
        mockClient.Setup(c => c.SendAsync(
                It.IsAny<SendRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(TestCallHelpers.CreateAsyncUnaryCall(new Empty()));

        await using var app = await CreateValidationTestAppAsync(mockClient.Object);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

        // Act
        var response = await client.PostAsJsonAsync(EmailEndpoints.Route, ValidModel);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EmailSendResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.ResultCode.Should().Be(EmailSendResultCode.Success);

        mockClient.Verify(c => c.SendAsync(
            It.IsAny<SendRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    public static TheoryData<object, string> InvalidEmailPayloads() => new()
    {
        { new { Name = "", Email = "alice@example.com", Subject = "Hello", Message = "Test message content" }, nameof(EmailFormModel.Name) },
        { new { Name = "Al", Email = "alice@example.com", Subject = "Hello", Message = "Test message content" }, nameof(EmailFormModel.Name) },
        { new { Name = "Alice", Email = "not-an-email", Subject = "Hello", Message = "Test message content" }, nameof(EmailFormModel.Email) },
        { new { Name = "Alice", Email = "alice@example.com", Subject = "A", Message = "Test message content" }, nameof(EmailFormModel.Subject) },
        { new { Name = "Alice", Email = "alice@example.com", Subject = "Hello", Message = "Too short" }, nameof(EmailFormModel.Message) }
    };

    private static async Task<WebApplication> CreateValidationTestAppAsync(
        SendEmailToAdmin.SendEmailToAdminClient emailClient)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddValidation();
        builder.Services.AddSingleton(emailClient);

        var app = builder.Build();
        app.MapPost(EmailEndpoints.Route, EmailEndpoints.SendEmail);
        await app.StartAsync();
        return app;
    }
}
