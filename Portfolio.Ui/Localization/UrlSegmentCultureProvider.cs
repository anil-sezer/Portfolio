using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace Portfolio.Ui.Localization;

/// <summary>
/// Determines the request culture from the first URL path segment.
/// "/tr" or "/tr/..." → tr-TR; everything else → en-US (default).
/// This makes each language accessible at its own URL for SEO.
/// </summary>
public class UrlSegmentCultureProvider : IRequestCultureProvider
{
    public Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/tr", StringComparison.OrdinalIgnoreCase) &&
            (path.Length == 3 || path[3] == '/'))
        {
            return Task.FromResult<ProviderCultureResult?>(
                new ProviderCultureResult("tr-TR"));
        }

        // Default to English for all other paths
        return Task.FromResult<ProviderCultureResult?>(
            new ProviderCultureResult("en-US"));
    }
}
