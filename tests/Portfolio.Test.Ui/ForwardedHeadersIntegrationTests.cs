using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Portfolio.Ui.Extensions;

namespace Portfolio.Test.Ui;

public class ForwardedHeadersIntegrationTests
{
    private static ForwardedHeadersMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.244.0.0/16"));
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.96.0.0/12"));
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("127.0.0.1/8"));
        options.KnownProxies.Add(IPAddress.IPv6Loopback);

        return new ForwardedHeadersMiddleware(
            next ?? (_ => Task.CompletedTask),
            NullLoggerFactory.Instance,
            Options.Create(options));
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestFromTrustedClusterProxy_ResolvesClientIpAndScheme()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse("10.244.1.6") // Traefik pod IP
            }
        };
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.195";
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        // Act
        await middleware.Invoke(context);

        // Assert
        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("203.0.113.195"));
        context.GetClientIpAddress().Should().Be("203.0.113.195");
        context.Request.Scheme.Should().Be("https");
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestFromTrustedClusterProxyWithIPv6MappedIPv4_ResolvesClientIp()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse("::ffff:10.244.1.6") // Kestrel dual-mode socket mapping
            }
        };
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.195";

        // Act
        await middleware.Invoke(context);

        // Assert
        context.GetClientIpAddress().Should().Be("203.0.113.195");
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestFromUntrustedIp_IgnoresSpoofedXForwardedFor()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse("198.51.100.1") // Untrusted external IP
            }
        };
        context.Request.Headers["X-Forwarded-For"] = "1.1.1.1";

        // Act
        await middleware.Invoke(context);

        // Assert
        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("198.51.100.1"));
        context.GetClientIpAddress().Should().Be("198.51.100.1");
    }

    [Fact]
    public async Task InvokeAsync_WhenClientSendsMultipleIps_ResolvesMostRecentUntrustedHop()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse("10.244.1.6") // Traefik pod
            }
        };
        // Simulated: Client sends spoofed "1.1.1.1", Traefik appends client's real IP "203.0.113.50"
        context.Request.Headers["X-Forwarded-For"] = "1.1.1.1, 203.0.113.50";

        // Act
        await middleware.Invoke(context);

        // Assert
        // With ForwardLimit = 1, the most recent untrusted hop (real client IP) is selected
        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("203.0.113.50"));
        context.GetClientIpAddress().Should().Be("203.0.113.50");
    }
}
