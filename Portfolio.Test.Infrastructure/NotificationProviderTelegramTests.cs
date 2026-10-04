using System.Net;
using FluentAssertions;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces.ThirdPartyServices.Dtos;
using Portfolio.Infrastructure.ThirdPartyServices;
using Portfolio.Test.Shared;

namespace Portfolio.Test.Infrastructure;

[Collection("EnvironmentVariables")]
public class NotificationProviderTelegramTests : IDisposable
{
    private readonly string? _origApiKey;
    private readonly string? _origChatId;

    public NotificationProviderTelegramTests()
    {
        _origApiKey = Environment.GetEnvironmentVariable("NOTIFICATION_TELEGRAM_API_KEY");
        _origChatId = Environment.GetEnvironmentVariable("NOTIFICATION_TELEGRAM_CHAT_ID");

        Environment.SetEnvironmentVariable("NOTIFICATION_TELEGRAM_API_KEY", "test_bot_token_123");
        Environment.SetEnvironmentVariable("NOTIFICATION_TELEGRAM_CHAT_ID", "12345678");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("NOTIFICATION_TELEGRAM_API_KEY", _origApiKey);
        Environment.SetEnvironmentVariable("NOTIFICATION_TELEGRAM_CHAT_ID", _origChatId);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenHttpSucceeds_ReturnsSuccessResultAndPostsCorrectPayload()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new MockHttpMessageHandler(async (req, ct) =>
        {
            capturedRequest = req;
            capturedBody = await req.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler);
        var provider = new NotificationProviderTelegram(httpClient);

        var dto = new NotificationDto
        {
            Name = "Alice",
            EmailAddress = "alice@example.com",
            Subject = "Test Subject",
            Message = "Hello World!",
            NotificationType = NotificationType.Info
        };

        // Act
        var result = await provider.SendNotificationAsync(dto);

        // Assert
        result.IsItSentSuccessfully.Should().BeTrue();
        result.ErrorMessage.Should().Contain("OK");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("https://api.telegram.org/bottest_bot_token_123/sendMessage");
        capturedRequest.Method.Should().Be(HttpMethod.Post);

        capturedBody.Should().NotBeNull();
        var decodedBody = Uri.UnescapeDataString(capturedBody!);
        decodedBody.Should().Contain("chat_id=12345678");
        decodedBody.Should().Contain("parse_mode=MarkdownV2");
        decodedBody.Should().Contain("[INFO]");
        decodedBody.Should().Contain("Alice");
        decodedBody.Should().Contain(@"alice@example\.com");
    }

    [Fact]
    public async Task SendNotificationAsync_WhenHttpFails_ReturnsFailureResult()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler);
        var provider = new NotificationProviderTelegram(httpClient);

        var dto = new NotificationDto
        {
            Name = "Bob",
            EmailAddress = "bob@example.com",
            Subject = "Error Test",
            Message = "Something broke.",
            NotificationType = NotificationType.Error
        };

        // Act
        var result = await provider.SendNotificationAsync(dto);

        // Assert
        result.IsItSentSuccessfully.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendNotificationAsync_WhenHttpClientThrowsException_ReturnsFailureResult()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network failure"));
        var httpClient = new HttpClient(handler);
        var provider = new NotificationProviderTelegram(httpClient);

        var dto = new NotificationDto
        {
            Name = "Charlie",
            EmailAddress = "charlie@example.com",
            Subject = "Network Error",
            Message = "Test message",
            NotificationType = NotificationType.Warning
        };

        // Act
        var result = await provider.SendNotificationAsync(dto);

        // Assert
        result.IsItSentSuccessfully.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Network failure");
    }

    [Theory]
    [InlineData(NotificationType.Info, "*[INFO]*", "ℹ️")]
    [InlineData(NotificationType.Success, "*[SUCCESS]*", "✅")]
    [InlineData(NotificationType.Error, "*[ERROR]*", "❌")]
    [InlineData(NotificationType.Warning, "*[WARNING]*", "⚠️")]
    [InlineData(NotificationType.Fatal, "*[FATAL]*", "🚨🚨🚨")]
    public async Task SendNotificationAsync_FormatsPrefixAndEmojiForNotificationTypes(
        NotificationType type,
        string expectedPrefix,
        string expectedEmoji)
    {
        // Arrange
        string? capturedBody = null;
        var handler = new MockHttpMessageHandler(async (req, ct) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler);
        var provider = new NotificationProviderTelegram(httpClient);

        var dto = new NotificationDto
        {
            Name = "User",
            EmailAddress = "user@example.com",
            Subject = "Type Test",
            Message = "Testing type",
            NotificationType = type
        };

        // Act
        var result = await provider.SendNotificationAsync(dto);

        // Assert
        result.IsItSentSuccessfully.Should().BeTrue();
        var decodedBody = Uri.UnescapeDataString(capturedBody!);
        decodedBody.Should().Contain(expectedEmoji);
        decodedBody.Should().Contain(expectedPrefix);
    }

    [Fact]
    public async Task SendNotificationAsync_EscapesMarkdownCharactersInPayload()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new MockHttpMessageHandler(async (req, ct) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler);
        var provider = new NotificationProviderTelegram(httpClient);

        // Contains markdown special characters: . ! _ - * [ ] ( )
        var dto = new NotificationDto
        {
            Name = "User",
            EmailAddress = "user@test.org",
            Subject = "Important! Read this [urgent]",
            Message = "Price is 100.50$ (item-1) with *bold* _italic_!",
            NotificationType = NotificationType.Info
        };

        // Act
        await provider.SendNotificationAsync(dto);

        // Assert
        var decodedBody = Uri.UnescapeDataString(capturedBody!);
        // In MarkdownV2, dots, exclamation marks, etc. in formatted message should be escaped with \
        decodedBody.Should().Contain(@"\.");
        decodedBody.Should().Contain(@"\!");
        decodedBody.Should().Contain(@"\(");
        decodedBody.Should().Contain(@"\)");
    }

    [Fact]
    public async Task SendNotificationAsync_WhenCancellationTokenCancelled_ReturnsFailureResult()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new MockHttpMessageHandler(async (req, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler);
        var provider = new NotificationProviderTelegram(httpClient);

        var dto = new NotificationDto
        {
            Name = "Cancelled",
            EmailAddress = "test@example.com",
            Subject = "Test",
            Message = "Cancel me",
            NotificationType = NotificationType.Info
        };

        // Act
        var result = await provider.SendNotificationAsync(dto, cts.Token);

        // Assert
        result.IsItSentSuccessfully.Should().BeFalse();
    }

    [Fact]
    public async Task SendNotificationAsync_WhenNotificationTypeIsUnknown_UsesFallbackEmojiAndPrefix()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new MockHttpMessageHandler(async (req, ct) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler);
        var provider = new NotificationProviderTelegram(httpClient);

        var dto = new NotificationDto
        {
            Name = "Fallback",
            EmailAddress = "fallback@example.com",
            Subject = "Unknown Type",
            Message = "Testing unknown",
            NotificationType = (NotificationType)999
        };

        // Act
        var result = await provider.SendNotificationAsync(dto);

        // Assert
        result.IsItSentSuccessfully.Should().BeTrue();
        var decodedBody = Uri.UnescapeDataString(capturedBody!);
        decodedBody.Should().Contain("📌");
        decodedBody.Should().Contain(@"*[LOG]*");
    }

    [Fact]
    public async Task SendNotificationAsync_EscapesAllMarkdownV2SpecialCharacters()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new MockHttpMessageHandler(async (req, ct) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler);
        var provider = new NotificationProviderTelegram(httpClient);

        var dto = new NotificationDto
        {
            Name = "SpecialChars",
            EmailAddress = "spec@example.com",
            Subject = "All Chars",
            Message = "Chars: ~ # + = | { } > -",
            NotificationType = NotificationType.Info
        };

        // Act
        await provider.SendNotificationAsync(dto);

        // Assert
        var decodedBody = Uri.UnescapeDataString(capturedBody!);
        decodedBody.Should().Contain(@"\~");
        decodedBody.Should().Contain(@"\#");
        decodedBody.Should().Contain(@"\+");
        decodedBody.Should().Contain(@"\=");
        decodedBody.Should().Contain(@"\|");
        decodedBody.Should().Contain(@"\{");
        decodedBody.Should().Contain(@"\}");
        decodedBody.Should().Contain(@"\>");
        decodedBody.Should().Contain(@"\-");
    }
}
