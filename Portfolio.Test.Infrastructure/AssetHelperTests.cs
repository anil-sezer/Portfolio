using FluentAssertions;
using Portfolio.Infrastructure.Helpers;

namespace Portfolio.Test.Infrastructure;

public class AssetHelperTests
{
    [Theory]
    [InlineData("/css/site.css")]
    [InlineData("/css/bootstrap.min.css")]
    [InlineData("/js/app.js")]
    [InlineData("/js/vendor/jquery.js")]
    [InlineData("/_content/SharedLibrary/script.js")]
    [InlineData("/_framework/blazor.web.js")]
    public void IsStaticAsset_WhenPathMatchesStaticPrefix_ReturnsTrue(string path)
    {
        // Act
        var result = AssetHelper.IsStaticAsset(path);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("/styles/main.css")]
    [InlineData("/scripts/main.js")]
    [InlineData("/favicon.ico")]
    [InlineData("/images/anim.gif")]
    [InlineData("/images/logo.png")]
    [InlineData("/images/photo.jpg")]
    [InlineData("/images/photo.jpeg")]
    [InlineData("/images/icon.svg")]
    [InlineData("/images/hero.webp")]
    [InlineData("/fonts/open-sans.woff")]
    [InlineData("/fonts/open-sans.woff2")]
    [InlineData("/wasm/runtime.wasm")]
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
    [InlineData("/JS/script.JS")]
    [InlineData("/_CONTENT/library.css")]
    [InlineData("/_FRAMEWORK/blazor.BOOT.JSON.js")]
    [InlineData("/images/LOGO.PNG")]
    [InlineData("/images/HERO.WEBP")]
    [InlineData("/fonts/FONT.WOFF2")]
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
    [InlineData("/contact")]
    [InlineData("/api/email/send")]
    [InlineData("/api/visits/log")]
    [InlineData("/tr")]
    [InlineData("/tr/about")]
    [InlineData("/dashboard")]
    [InlineData("/privacy-policy")]
    [InlineData("/admin")]
    [InlineData("/api/users")]
    [InlineData("/en/blog")]
    [InlineData("/page#css")]
    [InlineData("/styles/main")]
    [InlineData("/javascript")]
    public void IsStaticAsset_WhenPathIsNotStatic_ReturnsFalse(string path)
    {
        // Act
        var result = AssetHelper.IsStaticAsset(path);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("assets/logo.png")]
    [InlineData("styles/site.css")]
    [InlineData("scripts/bundle.js")]
    public void IsStaticAsset_WhenPathWithoutLeadingSlashEndsWithStaticExtension_ReturnsTrue(string path)
    {
        // Act
        var result = AssetHelper.IsStaticAsset(path);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("/json")]
    [InlineData("/jsp")]
    [InlineData("/css3")]
    public void IsStaticAsset_MatchesPrefix_EvenWhenFollowedByOtherCharacters(string path)
    {
        // Documenting AssetHelper implementation: StartsWith("/js") and StartsWith("/css") match any path starting with those tokens
        AssetHelper.IsStaticAsset(path).Should().BeTrue();
    }
}
