using Microsoft.Net.Http.Headers;
using Portfolio.Infrastructure.Constants;
using Portfolio.Ui.Extensions;
using Portfolio.Ui.Services;

namespace Portfolio.Ui.Endpoints;

public static class HeroImageEndpoints
{
    public const string RoutePattern = "/api/hero-image/{variant}";

    public static void MapHeroImageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(RoutePattern, ServeHeroImage)
            .WithName("ServeHeroImage")
            .RequireRateLimiting(RateLimiterExtensions.HeroImagePolicy);
    }

    internal static async Task<IResult> ServeHeroImage(
        string variant,
        string? v,
        BackgroundImageService backgroundImageService,
        IWebHostEnvironment env,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
         var normalizedVariant = string.Equals(variant, DeviceTypes.Mobile, StringComparison.OrdinalIgnoreCase)
            ? DeviceTypes.Mobile
            : DeviceTypes.Desktop;
        var currentImage = await backgroundImageService.GetFromCacheAsync(cancellationToken);
        var (bytes, isStale) = await backgroundImageService.GetHeroImageBytesAsync(normalizedVariant, cancellationToken);

        if (bytes is { Length: > 0 })
        {
            if (isStale)
                httpContext.Response.Headers[HeaderNames.CacheControl] = "public, max-age=300";
            else if (!string.IsNullOrWhiteSpace(v) && string.Equals(v, currentImage.Version, StringComparison.Ordinal))
                httpContext.Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
            else if (!string.IsNullOrWhiteSpace(v))
                httpContext.Response.Headers[HeaderNames.CacheControl] = "public, max-age=300";
            else
                httpContext.Response.Headers[HeaderNames.CacheControl] = "public, max-age=86400";

            return Results.Bytes(bytes, "image/webp");
        }

        var fallbackFileName = normalizedVariant == DeviceTypes.Mobile ? "default-bg-mobile.webp" : "default-bg.webp";
        var fallbackPath = Path.Combine(env.WebRootPath, "img", fallbackFileName);

        if (File.Exists(fallbackPath))
        {
            httpContext.Response.Headers[HeaderNames.CacheControl] = "public, max-age=300";
            return Results.File(fallbackPath, "image/webp");
        }

        return Results.NotFound();
    }
}
