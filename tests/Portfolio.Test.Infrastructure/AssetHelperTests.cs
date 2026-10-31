using FluentAssertions;
using Portfolio.Infrastructure.Helpers;

namespace Portfolio.Test.Infrastructure;

public class AssetHelperTests
{
    [Theory]
    [InlineData("/css/site.css")]
    [InlineData("/js/app.js")]
    [InlineData("/_framework/blazor.web.js")]
    public void IsStaticAsset_WhenPathMatchesStaticPrefix_ReturnsTrue(string path)
    {
        // Act
        var result = AssetHelper.IsStaticAsset(path);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("/images/logo.png")]
    [InlineData("/favicon.ico")]
    [InlineData("/fonts/open-sans.woff2")]
    [InlineData("/scripts/bundle.js.map")]
    public void IsStaticAsset_WhenPathMatchesStaticExtension_ReturnsTrue(string path)
    {
        // Act
        var result = AssetHelper.IsStaticAsset(path);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("/CSS/main.css")]
    [InlineData("/images/LOGO.PNG")]
    public void IsStaticAsset_IsCaseInsensitive(string path)
    {
        // Act
        var result = AssetHelper.IsStaticAsset(path);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/about")]
    [InlineData("/api/email/send")]
    public void IsStaticAsset_WhenPathIsNotStatic_ReturnsFalse(string path)
    {
        // Act
        var result = AssetHelper.IsStaticAsset(path);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("assets/logo.png")]
    public void IsStaticAsset_WhenPathWithoutLeadingSlashEndsWithStaticExtension_ReturnsTrue(string path)
    {
        // Act
        var result = AssetHelper.IsStaticAsset(path);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("/json")]
    public void IsStaticAsset_MatchesPrefix_EvenWhenFollowedByOtherCharacters(string path)
    {
        // Documenting AssetHelper implementation: StartsWith("/js") and StartsWith("/css") match any path starting with those tokens
        AssetHelper.IsStaticAsset(path).Should().BeTrue();
    }
}
