using System.Text.Json;
using Portfolio.Infrastructure.Constants;

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
            MaxTouchPoints      = GetValueOrDefault(viaJavascript, "maxTouchPoints", DefaultValues.EmptyForInt),
            IpAddress           = GetIpAddress(httpContextAccessor),
            RequestedUrl        = GetRequestedPage(httpContextAccessor),
            Extras              = GetAllRequestHeadersAsJson(httpContextAccessor)
        };
    }

    public static StoreVisitorInfoRequest Create(IHttpContextAccessor httpContextAccessor)
    {
        return new StoreVisitorInfoRequest
        {
            Language            = string.Empty,
            Platform            = string.Empty,
            Referrer            = string.Empty,
            UserAgent           = string.Empty,
            DoNotTrack          = string.Empty,
            Connection          = string.Empty,
            Resolution          = string.Empty,
            DeviceMemory        = string.Empty,
            OnLine              = false,
            HardwareConcurrency = string.Empty,
            Webdriver           = false,
            CookieEnabled       = false,
            MaxTouchPoints      = DefaultValues.EmptyForInt,
            IpAddress           = GetIpAddress(httpContextAccessor),
            RequestedUrl        = GetRequestedPage(httpContextAccessor),
            Extras              = GetAllRequestHeadersAsJson(httpContextAccessor)
        };
    }

    private static string GetValueOrDefault(Dictionary<string, string> data, string key, string defaultValue = "")
        => data.TryGetValue(key, out var value) ? value : defaultValue;

    private static T GetValueOrDefault<T>(Dictionary<string, string> data, string key, T defaultValue = default)
    {
        if (data.TryGetValue(key, out var value) && value is not null)
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch(Exception e)
            {
                Log.Warning(e, "Error while converting {Value} to {Type}", value, typeof(T));
                return defaultValue; // Prevent crashes on invalid types
            }
        }
        return defaultValue;
    }

    private static string GetIpAddress(IHttpContextAccessor httpContextAccessor)
    {
        var headers = httpContextAccessor.HttpContext?.Request.Headers;
        if (headers is null)
            return "";
        
        return headers.TryGetValue("X-Real-IP", out var value) ? value.ToString() : "";
    }
    
    private static string GetRequestedPage(IHttpContextAccessor httpContextAccessor)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
            return "";
    
        var fullUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
    
        return fullUrl;
    }

    private static string GetAllRequestHeadersAsJson(IHttpContextAccessor httpContextAccessor)
    {
        try
        {
            var headers = httpContextAccessor.HttpContext?.Request.Headers;
            return JsonSerializer.Serialize(headers);
        }
        catch (Exception e)
        {
            Log.Error("Error while serializing headers: {Error}", e.Message);
            return "";
        }
    }
}
