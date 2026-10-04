using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Portfolio.Infrastructure.Constants;
using Portfolio.Ui.Factories;

namespace Portfolio.Test.Ui;

public class ClientInfoRequestFactoryTests
{
    private static (DefaultHttpContext context, Mock<IHttpContextAccessor> mockAccessor) CreateTestHttpContext(
        string scheme = "https",
        string host = "anilsezer.net",
        string path = "/projects",
        string queryString = "?ref=test")
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Scheme = scheme,
                Host = new HostString(host),
                Path = path,
                QueryString = new QueryString(queryString)
            },
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse("127.0.0.1")
            }
        };

        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(context);

        return (context, mockAccessor);
    }

    [Fact]
    public void Create_WithJavaScriptDictionary_ParsesAllFieldsCorrectly()
    {
        // Arrange
        var (_, mockAccessor) = CreateTestHttpContext();
        var jsData = new Dictionary<string, string>
        {
            { "language", "en-US" },
            { "platform", "Win32" },
            { "referrer", "https://google.com" },
            { "userAgent", "Mozilla/5.0 Chrome" },
            { "doNotTrack", "1" },
            { "connection", "4g" },
            { "resolution", "1920x1080" },
            { "deviceMemory", "16" },
            { "onLine", "True" },
            { "hardwareConcurrency", "12" },
            { "webdriver", "False" },
            { "cookieEnabled", "True" },
            { "maxTouchPoints", "10" }
        };

        // Act
        var result = ClientInfoRequestFactory.Create(jsData, mockAccessor.Object);

        // Assert
        result.Language.Should().Be("en-US");
        result.Platform.Should().Be("Win32");
        result.Referrer.Should().Be("https://google.com");
        result.UserAgent.Should().Be("Mozilla/5.0 Chrome");
        result.DoNotTrack.Should().Be("1");
        result.Connection.Should().Be("4g");
        result.Resolution.Should().Be("1920x1080");
        result.DeviceMemory.Should().Be("16");
        result.OnLine.Should().BeTrue();
        result.HardwareConcurrency.Should().Be("12");
        result.Webdriver.Should().BeFalse();
        result.CookieEnabled.Should().BeTrue();
        result.MaxTouchPoints.Should().Be(10);
        result.RequestedUrl.Should().Be("https://anilsezer.net/projects?ref=test");
    }

    [Fact]
    public void Create_WithInvalidJsTypes_FallsBackToDefaultsWithoutCrashing()
    {
        // Arrange
        var (_, mockAccessor) = CreateTestHttpContext();
        var jsData = new Dictionary<string, string>
        {
            { "onLine", "not-a-bool" },
            { "webdriver", "invalid" },
            { "maxTouchPoints", "not-an-int" }
        };

        // Act
        var result = ClientInfoRequestFactory.Create(jsData, mockAccessor.Object);

        // Assert
        result.OnLine.Should().BeFalse();
        result.Webdriver.Should().BeFalse();
        result.MaxTouchPoints.Should().Be(-1);
    }

    [Fact]
    public void Create_WithoutJs_PopulatesFromHttpHeaders()
    {
        // Arrange
        var (context, mockAccessor) = CreateTestHttpContext();
        context.Request.Headers.AcceptLanguage = "tr-TR,tr;q=0.9";
        context.Request.Headers["Sec-CH-UA-Platform"] = "\"Linux\"";
        context.Request.Headers.Referer = "https://github.com";
        context.Request.Headers.UserAgent = "CustomBrowser/1.0";
        context.Request.Headers["DNT"] = "1";

        // Act
        var result = ClientInfoRequestFactory.Create(mockAccessor.Object);

        // Assert
        result.Language.Should().Be("tr-TR,tr;q=0.9");
        result.Platform.Should().Be("Linux");
        result.Referrer.Should().Be("https://github.com");
        result.UserAgent.Should().Be("CustomBrowser/1.0");
        result.DoNotTrack.Should().Be("1");
        result.RequestedUrl.Should().Be("https://anilsezer.net/projects?ref=test");
    }

    [Fact]
    public void Create_StripsUnsafeHeadersAndPreservesSafeHeadersInExtras()
    {
        // Arrange
        var (context, mockAccessor) = CreateTestHttpContext();
        // Unsafe headers
        context.Request.Headers.Cookie = "session=secret_value";
        context.Request.Headers.SetCookie = "foo=bar";
        context.Request.Headers.Authorization = "Bearer eyJhbGciOi...";
        context.Request.Headers.ProxyAuthorization = "Basic dXNlcjpwYXNz";
        context.Request.Headers[HeaderConstants.XsrfToken] = "csrf-token-123";
        context.Request.Headers["X-XSRF-TOKEN"] = "xsrf-456";
        context.Request.Headers["RequestVerificationToken"] = "token-789";
        context.Request.Headers["X-Api-Key"] = "api-key-abc";
        context.Request.Headers["My-Custom-Secret"] = "shh";
        context.Request.Headers["App-Password"] = "super-secret";
        context.Request.Headers["Admin-Auth-Header"] = "admin";

        // Safe headers
        context.Request.Headers.Host = "anilsezer.net";
        context.Request.Headers.Accept = "text/html";
        context.Request.Headers.UserAgent = "TestRunner/2.0";

        // Act
        var result = ClientInfoRequestFactory.Create(mockAccessor.Object);

        // Assert
        result.Extras.Should().NotBeEmpty();
        var parsedHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(result.Extras)!;

        // Unsafe headers must NOT be present
        parsedHeaders.Should().NotContainKey("Cookie");
        parsedHeaders.Should().NotContainKey("Set-Cookie");
        parsedHeaders.Should().NotContainKey("Authorization");
        parsedHeaders.Should().NotContainKey("Proxy-Authorization");
        parsedHeaders.Should().NotContainKey(HeaderConstants.XsrfToken);
        parsedHeaders.Should().NotContainKey("X-XSRF-TOKEN");
        parsedHeaders.Should().NotContainKey("RequestVerificationToken");
        parsedHeaders.Should().NotContainKey("X-Api-Key");
        parsedHeaders.Should().NotContainKey("My-Custom-Secret");
        parsedHeaders.Should().NotContainKey("App-Password");
        parsedHeaders.Should().NotContainKey("Admin-Auth-Header");

        // Safe headers must be present
        parsedHeaders.Should().ContainKey("Host");
        parsedHeaders["Host"].Should().Be("anilsezer.net");
        parsedHeaders.Should().ContainKey("Accept");
        parsedHeaders.Should().ContainKey("User-Agent");
    }

    [Fact]
    public void Create_ResolvesClientIpFromXForwardedForWhenPresent()
    {
        // Arrange
        var (context, mockAccessor) = CreateTestHttpContext();
        context.Request.Headers[HeaderConstants.XForwardedFor] = "203.0.113.195, 70.41.3.18";

        // Act
        var result = ClientInfoRequestFactory.Create(mockAccessor.Object);

        // Assert
        result.IpAddress.Should().Be("203.0.113.195");
    }

    [Fact]
    public void Create_ResolvesClientIpFromXRealIpWhenXForwardedForMissing()
    {
        // Arrange
        var (context, mockAccessor) = CreateTestHttpContext();
        context.Request.Headers[HeaderConstants.XRealIp] = "198.51.100.77";

        // Act
        var result = ClientInfoRequestFactory.Create(mockAccessor.Object);

        // Assert
        result.IpAddress.Should().Be("198.51.100.77");
    }

    [Fact]
    public void Create_FallsBackToRemoteIpAddressWhenNoProxyHeaders()
    {
        // Arrange
        var (context, mockAccessor) = CreateTestHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.42");

        // Act
        var result = ClientInfoRequestFactory.Create(mockAccessor.Object);

        // Assert
        result.IpAddress.Should().Be("10.0.0.42");
    }

    [Fact]
    public void Create_WhenHttpContextIsNull_ReturnsDefaultsGracefully()
    {
        // Arrange
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns((HttpContext)null!);

        // Act
        var result = ClientInfoRequestFactory.Create(mockAccessor.Object);

        // Assert
        result.Should().NotBeNull();
        result.IpAddress.Should().Be("unknown");
        result.RequestedUrl.Should().BeEmpty();
        result.Extras.Should().Be("{}");
        result.Language.Should().BeEmpty();
        result.Platform.Should().BeEmpty();
        result.Referrer.Should().BeEmpty();
    }

    [Fact]
    public void Create_WhenReferrerHeaderWithDoubleR_UsesReferrerHeader()
    {
        // Arrange
        var (context, mockAccessor) = CreateTestHttpContext();
        context.Request.Headers.Remove("Referer");
        context.Request.Headers["Referrer"] = "https://duckduckgo.com";

        // Act
        var result = ClientInfoRequestFactory.Create(mockAccessor.Object);

        // Assert
        result.Referrer.Should().Be("https://duckduckgo.com");
    }

    [Fact]
    public void Create_WithEmptyJavaScriptDictionary_ReturnsDefaultFallbackValues()
    {
        // Arrange
        var (_, mockAccessor) = CreateTestHttpContext();
        var emptyJs = new Dictionary<string, string>();

        // Act
        var result = ClientInfoRequestFactory.Create(emptyJs, mockAccessor.Object);

        // Assert
        result.Language.Should().BeEmpty();
        result.Platform.Should().BeEmpty();
        result.OnLine.Should().BeFalse();
        result.Webdriver.Should().BeFalse();
        result.CookieEnabled.Should().BeFalse();
        result.MaxTouchPoints.Should().Be(-1);
    }

    [Fact]
    public void Create_StripsCustomSubstringUnsafeHeaders()
    {
        // Arrange
        var (context, mockAccessor) = CreateTestHttpContext();
        context.Request.Headers["Client-Token-Header"] = "val1";
        context.Request.Headers["My-Secret-Pass"] = "val2";
        context.Request.Headers["App-Password-Key"] = "val3";
        context.Request.Headers["External-ApiKey"] = "val4";
        context.Request.Headers["Custom-Auth-Token"] = "val5";
        context.Request.Headers["Session-Cookie-Id"] = "val6";
        context.Request.Headers["Safe-Custom-Header"] = "allowed";

        // Act
        var result = ClientInfoRequestFactory.Create(mockAccessor.Object);

        // Assert
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(result.Extras)!;
        parsed.Should().NotContainKey("Client-Token-Header");
        parsed.Should().NotContainKey("My-Secret-Pass");
        parsed.Should().NotContainKey("App-Password-Key");
        parsed.Should().NotContainKey("External-ApiKey");
        parsed.Should().NotContainKey("Custom-Auth-Token");
        parsed.Should().NotContainKey("Session-Cookie-Id");
        parsed.Should().ContainKey("Safe-Custom-Header");
    }

    [Fact]
    public void Create_ResolvesClientIpFromXForwardedFor_HandlesWhitespaceAndMultipleIps()
    {
        // Arrange
        var (context, mockAccessor) = CreateTestHttpContext();
        context.Request.Headers[HeaderConstants.XForwardedFor] = "  192.168.1.100  ,  10.0.0.1  ";

        // Act
        var result = ClientInfoRequestFactory.Create(mockAccessor.Object);

        // Assert
        result.IpAddress.Should().Be("192.168.1.100");
    }
}
