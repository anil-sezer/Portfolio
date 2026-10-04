using System.Collections.Frozen;
using System.Text.Json;
using Portfolio.Infrastructure.Constants;
using Portfolio.Ui.Extensions;

namespace Portfolio.Ui.Factories;

public static class ClientInfoRequestFactory
{
    public static StoreVisitorInfoRequest Create(Dictionary<string, string> viaJavascript, IHttpContextAccessor httpContextAccessor)
    {
        return new StoreVisitorInfoRequest
        {
            Language            = GetValueOrDefault(viaJavascript, "language"),
            Platform            = GetValueOrDefault(viaJavascript, "platform"),
            Referrer            = GetValueOrDefault(viaJavascript, "referrer"),
            UserAgent           = GetValueOrDefault(viaJavascript, "userAgent"),
            DoNotTrack          = GetValueOrDefault(viaJavascript, "doNotTrack"),
            Connection          = GetValueOrDefault(viaJavascript, "connection"),
            Resolution          = GetValueOrDefault(viaJavascript, "resolution"),
            DeviceMemory        = GetValueOrDefault(viaJavascript, "deviceMemory"),
            OnLine              = GetValueOrDefault(viaJavascript, "onLine", false),
            HardwareConcurrency = GetValueOrDefault(viaJavascript, "hardwareConcurrency"),
            Webdriver           = GetValueOrDefault(viaJavascript, "webdriver", false),
            CookieEnabled       = GetValueOrDefault(viaJavascript, "cookieEnabled", false),
            MaxTouchPoints      = GetValueOrDefault(viaJavascript, "maxTouchPoints", -1),
            IpAddress           = httpContextAccessor.GetClientIpAddress(),
            RequestedUrl        = GetRequestedPage(httpContextAccessor),
            Extras              = GetRequestHeadersAsJson(httpContextAccessor)
        };
    }

    public static StoreVisitorInfoRequest Create(IHttpContextAccessor httpContextAccessor)
    {
        var headers = httpContextAccessor.HttpContext?.Request.Headers;

        return new StoreVisitorInfoRequest
        {
            Language            = GetHeaderValue(headers, "Accept-Language"),
            Platform            = GetHeaderValue(headers, "Sec-CH-UA-Platform").Trim('"'),
            Referrer            = GetReferrerHeader(headers),
            UserAgent           = GetHeaderValue(headers, "User-Agent"),
            DoNotTrack          = GetHeaderValue(headers, "DNT"),
            Connection          = string.Empty,
            Resolution          = string.Empty,
            DeviceMemory        = string.Empty,
            OnLine              = false,
            HardwareConcurrency = string.Empty,
            Webdriver           = false,
            CookieEnabled       = false,
            MaxTouchPoints      = -1,
            IpAddress           = httpContextAccessor.GetClientIpAddress(),
            RequestedUrl        = GetRequestedPage(httpContextAccessor),
            Extras              = GetRequestHeadersAsJson(httpContextAccessor)
        };
    }

    private static string GetHeaderValue(IHeaderDictionary? headers, string headerName)
    {
        if (headers is not null && headers.TryGetValue(headerName, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.ToString();
        }

        return string.Empty;
    }

    private static string GetReferrerHeader(IHeaderDictionary? headers)
    {
        if (headers is null)
        {
            return string.Empty;
        }

        if (headers.TryGetValue("Referer", out var referer) && !string.IsNullOrWhiteSpace(referer))
        {
            return referer.ToString();
        }

        if (headers.TryGetValue("Referrer", out var referrer) && !string.IsNullOrWhiteSpace(referrer))
        {
            return referrer.ToString();
        }

        return string.Empty;
    }

    private static readonly FrozenSet<string> UnsafeHeaders = new[]
    {
        "Cookie",
        "Set-Cookie",
        "Authorization",
        "Proxy-Authorization",
        "Proxy-Authenticate",
        "WWW-Authenticate",
        HeaderConstants.XsrfToken,
        "X-XSRF-TOKEN",
        "RequestVerificationToken",
        "__RequestVerificationToken",
        "X-Api-Key",
        "ApiKey",
        "X-Auth-Token",
        "X-Access-Token",
        "Token",
        "Secret",
        "X-Secret",
        "X-ARR-ClientCert",
        "X-Forwarded-Tls-Client-Cert"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static bool IsUnsafeHeader(string headerName)
    {
        if (string.IsNullOrWhiteSpace(headerName))
        {
            return true;
        }

        if (UnsafeHeaders.Contains(headerName))
        {
            return true;
        }

        if (headerName.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            headerName.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
            headerName.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            headerName.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            headerName.Contains("apikey", StringComparison.OrdinalIgnoreCase) ||
            headerName.Contains("cookie", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string GetValueOrDefault(Dictionary<string, string> data, string key, string defaultValue = "")
        => data.TryGetValue(key, out var value) ? value : defaultValue;

    private static T? GetValueOrDefault<T>(Dictionary<string, string> data, string key, T? defaultValue = default)
    {
        if (data.TryGetValue(key, out var value) && value is not null)
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception e)
            {
                Log.Warning(e, "Error while converting {Value} to {Type}", value, typeof(T));
                return defaultValue; // Prevent crashes on invalid types
            }
        }
        return defaultValue;
    }
    
    private static string GetRequestedPage(IHttpContextAccessor httpContextAccessor)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
            return "";
    
        var fullUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
    
        return fullUrl;
    }

    private static string GetRequestHeadersAsJson(IHttpContextAccessor httpContextAccessor)
    {
        try
        {
            var headers = httpContextAccessor.HttpContext?.Request.Headers;
            if (headers is null || headers.Count == 0)
            {
                return "{}";
            }

            var allowedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in headers)
            {
                if (!IsUnsafeHeader(key))
                    allowedHeaders[key] = value.ToString();
            }

            return JsonSerializer.Serialize(allowedHeaders);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while serializing request headers: {Error}", e.Message);
            return "{}";
        }
    }
}
