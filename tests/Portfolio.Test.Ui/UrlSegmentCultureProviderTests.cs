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
    [InlineData("/TR/about", "tr-TR")]
    [InlineData("/travel", "en-US")]
    [InlineData("/tr-TR", "en-US")]
    [InlineData("/", "en-US")]
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
