using System.Net;
using System.Net.Sockets;

namespace Portfolio.Grpc.Security;

public class SafeUrlValidator
{
    private static readonly string[] ProhibitedHostSuffixes = [".local", ".internal", ".lan", ".home.arpa"];

    private readonly Func<string, CancellationToken, Task<IPAddress[]>> _dnsResolver;
    private readonly ILogger<SafeUrlValidator> _logger;

    public SafeUrlValidator(
        ILogger<SafeUrlValidator> logger,
        Func<string, CancellationToken, Task<IPAddress[]>>? dnsResolver = null)
    {
        _logger = logger;
        _dnsResolver = dnsResolver ?? ((host, ct) => Dns.GetHostAddressesAsync(host, ct));
    }

    public virtual async Task<bool> IsSafePublicUrlAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("Rejecting URL '{Url}': Not a valid absolute URI.", url);
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            _logger.LogWarning("Rejecting URL '{Url}': Scheme '{Scheme}' is not HTTP or HTTPS.", url, uri.Scheme);
            return false;
        }

        var host = uri.DnsSafeHost.TrimEnd('.');
        if (string.IsNullOrWhiteSpace(host))
            return false;

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Rejecting URL '{Url}': Host refers to localhost.", url);
            return false;
        }

        foreach (var suffix in ProhibitedHostSuffixes)
        {
            if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Rejecting URL '{Url}': Host ends with internal suffix '{Suffix}'.", url, suffix);
                return false;
            }
        }

        if (IPAddress.TryParse(host, out var directIp))
        {
            if (IsPrivateOrReservedIp(directIp))
            {
                _logger.LogWarning("Rejecting URL '{Url}': IP '{Ip}' is private or reserved.", url, directIp);
                return false;
            }

            return true;
        }

        try
        {
            var resolvedIps = await _dnsResolver(host, cancellationToken);
            if (resolvedIps.Length == 0)
            {
                _logger.LogWarning("Rejecting URL '{Url}': DNS resolution returned no addresses.", url);
                return false;
            }

            foreach (var ip in resolvedIps)
            {
                if (IsPrivateOrReservedIp(ip))
                {
                    _logger.LogWarning("Rejecting URL '{Url}': Resolved IP '{Ip}' is private or reserved.", url, ip);
                    return false;
                }
            }

            return true;
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "Rejecting URL '{Url}': DNS resolution failed for host '{Host}'.", url, host);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rejecting URL '{Url}': Unexpected error during host validation.", url);
            return false;
        }
    }

    public static bool IsPrivateOrReservedIp(IPAddress ipAddress)
    {
        if (ipAddress.IsIPv4MappedToIPv6)
            ipAddress = ipAddress.MapToIPv4();

        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ipAddress.GetAddressBytes();

            // 0.0.0.0/8 (This host on this network)
            if (bytes[0] == 0)
                return true;

            // 127.0.0.0/8 (Loopback)
            if (bytes[0] == 127 || IPAddress.IsLoopback(ipAddress))
                return true;

            // 10.0.0.0/8 (RFC 1918 Private)
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12 (RFC 1918 Private)
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16 (RFC 1918 Private)
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 169.254.0.0/16 (Link Local / Cloud Metadata)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            // 100.64.0.0/10 (Carrier-Grade NAT)
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
                return true;

            // 198.18.0.0/15 (Network benchmark)
            if (bytes[0] == 198 && (bytes[1] == 18 || bytes[1] == 19))
                return true;

            // Documentation & Examples (TEST-NET-1, TEST-NET-2, TEST-NET-3)
            if (bytes[0] == 192 && bytes[1] == 0 && (bytes[2] == 0 || bytes[2] == 2))
                return true;
            if (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100)
                return true;
            if (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113)
                return true;

            // 224.0.0.0/4 (Multicast)
            if (bytes[0] >= 224 && bytes[0] <= 239)
                return true;

            // 240.0.0.0/4 (Reserved / Future Use) and 255.255.255.255 (Broadcast)
            if (bytes[0] >= 240)
                return true;

            return false;
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = ipAddress.GetAddressBytes();

            // ::1 (Loopback)
            if (IPAddress.IsLoopback(ipAddress))
                return true;

            // :: (Unspecified)
            if (ipAddress.Equals(IPAddress.IPv6None) || ipAddress.Equals(IPAddress.IPv6Any))
                return true;

            // fe80::/10 (Link-Local Unicast)
            if (ipAddress.IsIPv6LinkLocal || (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80))
                return true;

            // fec0::/10 (Site-Local)
            if (ipAddress.IsIPv6SiteLocal || (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0xc0))
                return true;

            // ff00::/8 (Multicast)
            if (ipAddress.IsIPv6Multicast || bytes[0] == 0xff)
                return true;

            // fc00::/7 (Unique Local Address - RFC 4193)
            if ((bytes[0] & 0xfe) == 0xfc)
                return true;

            return false;
        }

        return true;
    }
}
