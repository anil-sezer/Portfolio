using Portfolio.Infrastructure.Helpers;

namespace Portfolio.Ui.Models;

public class BackgroundImageModel
{
    public required string Url { get; init; }
    public required string AltText { get; init; }
    public required ImageOfTheDaySource Source { get; init; }
    
    public string Version => ImageVersionHelper.ComputeVersionHash(Url);

    public bool IsFallback => IsLocalFallback(Url);

    public string DesktopImageUrl => IsFallback
        ? "img/default-bg.webp"
        : $"/api/hero-image/desktop?v={Version}";

    public string MobileImageUrl => IsFallback
        ? "img/default-bg-mobile.webp"
        : $"/api/hero-image/mobile?v={Version}";

    private static bool IsLocalFallback(string url) =>
        string.IsNullOrWhiteSpace(url) ||
        !url.StartsWith("http", StringComparison.OrdinalIgnoreCase);
}