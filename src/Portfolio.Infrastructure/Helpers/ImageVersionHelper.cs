using System.Security.Cryptography;
using System.Text;

namespace Portfolio.Infrastructure.Helpers;

public static class ImageVersionHelper
{
    public static string ComputeVersionHash(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "default";

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexStringLower(bytes[..6]);
    }
}
