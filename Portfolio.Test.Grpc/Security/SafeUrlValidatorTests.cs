using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Grpc.Security;

namespace Portfolio.Test.Grpc.Security;

public class SafeUrlValidatorTests
{
    private static SafeUrlValidator CreateValidator(Func<string, CancellationToken, Task<IPAddress[]>>? dnsResolver = null)
    {
        return new SafeUrlValidator(NullLogger<SafeUrlValidator>.Instance, dnsResolver);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-url")]
    [InlineData("relative/path/image.jpg")]
    public async Task IsSafePublicUrlAsync_WhenUrlIsInvalidOrEmpty_ReturnsFalse(string? url)
    {
        var validator = CreateValidator();
        var result = await validator.IsSafePublicUrlAsync(url);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/image.jpg")]
    [InlineData("gopher://example.com/")]
    [InlineData("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA")]
    [InlineData("javascript:alert(1)")]
    public async Task IsSafePublicUrlAsync_WhenSchemeIsNotHttpOrHttps_ReturnsFalse(string url)
    {
        var validator = CreateValidator();
        var result = await validator.IsSafePublicUrlAsync(url);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("http://localhost/image.jpg")]
    [InlineData("https://localhost:8080/image.jpg")]
    [InlineData("http://localhost./image.jpg")]
    [InlineData("http://pi4-red.local/image.jpg")]
    [InlineData("http://backend.internal/image.jpg")]
    [InlineData("http://nas.lan/image.jpg")]
    [InlineData("http://router.home.arpa/image.jpg")]
    public async Task IsSafePublicUrlAsync_WhenHostIsLocalOrProhibitedSuffix_ReturnsFalse(string url)
    {
        var validator = CreateValidator();
        var result = await validator.IsSafePublicUrlAsync(url);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("http://127.0.0.1/image.jpg")]
    [InlineData("http://127.0.1.1/image.jpg")]
    [InlineData("http://0.0.0.0/image.jpg")]
    [InlineData("http://10.0.0.1/image.jpg")]
    [InlineData("http://10.244.1.5:8080/image.jpg")]
    [InlineData("http://172.16.0.1/image.jpg")]
    [InlineData("http://172.31.255.255/image.jpg")]
    [InlineData("http://192.168.1.120:8080/image.jpg")]
    [InlineData("http://192.168.0.1/image.jpg")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://100.64.0.1/image.jpg")]
    [InlineData("http://198.18.0.1/image.jpg")]
    [InlineData("http://224.0.0.1/image.jpg")]
    [InlineData("http://255.255.255.255/image.jpg")]
    [InlineData("http://[::1]/image.jpg")]
    [InlineData("http://[::]/image.jpg")]
    [InlineData("http://[fe80::1]/image.jpg")]
    [InlineData("http://[fc00::1]/image.jpg")]
    [InlineData("http://[fd12:3456:789a::1]/image.jpg")]
    public async Task IsSafePublicUrlAsync_WhenDirectIpIsPrivateOrReserved_ReturnsFalse(string url)
    {
        var validator = CreateValidator();
        var result = await validator.IsSafePublicUrlAsync(url);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("http://93.184.215.14/image.jpg")]
    [InlineData("https://8.8.8.8/image.jpg")]
    [InlineData("https://1.1.1.1/image.jpg")]
    public async Task IsSafePublicUrlAsync_WhenDirectIpIsPublic_ReturnsTrue(string url)
    {
        var validator = CreateValidator();
        var result = await validator.IsSafePublicUrlAsync(url);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSafePublicUrlAsync_WhenDnsResolvesToPublicIp_ReturnsTrue()
    {
        // Arrange
        var validator = CreateValidator((host, ct) => Task.FromResult(new[] { IPAddress.Parse("93.184.215.14") }));

        // Act
        var result = await validator.IsSafePublicUrlAsync("https://example.com/image.jpg");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.5")]
    [InlineData("192.168.1.120")]
    [InlineData("169.254.169.254")]
    [InlineData("::1")]
    public async Task IsSafePublicUrlAsync_WhenDnsResolvesToPrivateIp_RejectsDnsRebinding(string resolvedIp)
    {
        // Arrange
        var validator = CreateValidator((host, ct) => Task.FromResult(new[] { IPAddress.Parse(resolvedIp) }));

        // Act
        var result = await validator.IsSafePublicUrlAsync("https://attacker-rebinding.com/image.jpg");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSafePublicUrlAsync_WhenDnsThrowsSocketException_ReturnsFalse()
    {
        // Arrange
        var validator = CreateValidator((host, ct) => throw new SocketException((int)SocketError.HostNotFound));

        // Act
        var result = await validator.IsSafePublicUrlAsync("https://non-existent-domain-123456.com/image.jpg");

        // Assert
        result.Should().BeFalse();
    }
}
