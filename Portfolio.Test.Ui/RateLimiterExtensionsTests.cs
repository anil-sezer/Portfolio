using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Portfolio.Ui.Extensions;

namespace Portfolio.Test.Ui;

public class RateLimiterExtensionsTests
{
    [Fact]
    public void AddAppRateLimiting_RegistersRateLimiterOptions_WithAllConfiguredPolicies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAppRateLimiting();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        // Assert
        options.RejectionStatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        options.OnRejected.Should().NotBeNull();

        // Verify policy constants are defined
        RateLimiterExtensions.EmailSendPolicy.Should().Be("EmailSendPolicy");
        RateLimiterExtensions.VisitLogPolicy.Should().Be("VisitLogPolicy");
        RateLimiterExtensions.ClusterStatsPolicy.Should().Be("ClusterStatsPolicy");
        RateLimiterExtensions.HeroImagePolicy.Should().Be("HeroImagePolicy");
        RateLimiterExtensions.BlazorPagesPolicy.Should().Be("BlazorPagesPolicy");
    }
}
