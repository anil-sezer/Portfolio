using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Portfolio.Ui.Localization;

namespace Portfolio.Test.Ui;

public class UrlSegmentCultureProviderTests
{
    [Theory]
    [InlineData("/tr", "tr-TR")]
    [InlineData("/tr/", "tr-TR")]
    [InlineData("/tr/about", "tr-TR")]
    [InlineData("/tr/contact/us", "tr-TR")]
    [InlineData("/tr/projects/item-1", "tr-TR")]
    [InlineData("/TR", "tr-TR")]
    [InlineData("/TR/about", "tr-TR")]
    [InlineData("/tr-TR", "en-US")]
    [InlineData("/tr-tr", "en-US")]
    [InlineData("/trr", "en-US")]
    [InlineData("/travel", "en-US")]
    [InlineData("/training", "en-US")]
    [InlineData("/trend", "en-US")]
    [InlineData("/", "en-US")]
    [InlineData("", "en-US")]
    [InlineData("/en", "en-US")]
    [InlineData("/en/about", "en-US")]
    [InlineData("/about", "en-US")]
    [InlineData("/contact", "en-US")]
    public async Task DetermineProviderCultureResult_ReturnsExpectedCulture(string path, string expectedCulture)
    {
        // Arrange
        var provider = new UrlSegmentCultureProvider();
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        // Act
        var result = await provider.DetermineProviderCultureResult(context);

        // Assert
        result.Should().NotBeNull();
        result!.Cultures.Should().ContainSingle(c => c.Value == expectedCulture);
    }
}
