namespace Portfolio.Infrastructure.Helpers;

public static class AssetHelper
{
    public static bool IsStaticAsset(string path)
    {
        return path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".woff", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".map", StringComparison.OrdinalIgnoreCase);
    }
}
