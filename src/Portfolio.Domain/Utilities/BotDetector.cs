using System.Text.Json;
using System.Text.RegularExpressions;
using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Utilities;

public sealed record BotDetectionContext
{
    public string UserAgent { get; init; } = string.Empty;
    public bool Webdriver { get; init; }
    public string RequestedUrl { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public string AcceptLanguage { get; init; } = string.Empty;
    public string Resolution { get; init; } = string.Empty;
    public string Connection { get; init; } = string.Empty;
    public string DeviceMemory { get; init; } = string.Empty;
    public string HardwareConcurrency { get; init; } = string.Empty;
    public int MaxTouchPoints { get; init; } = -1;
    public bool CookieEnabled { get; init; }
    public bool OnLine { get; init; }
    public string Extras { get; init; } = string.Empty;

    public static BotDetectionContext FromRequestLog(RequestLog log) => new()
    {
        UserAgent = log.UserAgent,
        Webdriver = log.Webdriver,
        RequestedUrl = log.RequestedUrl,
        Platform = log.Platform,
        AcceptLanguage = log.AcceptLanguage,
        Resolution = log.Resolution,
        Connection = log.Connection,
        DeviceMemory = log.DeviceMemory,
        HardwareConcurrency = log.HardwareConcurrency,
        MaxTouchPoints = log.MaxTouchPoints,
        CookieEnabled = log.CookieEnabled,
        OnLine = log.OnLine,
        Extras = log.Extras
    };
}

public sealed record BotDetectionResult(bool IsBot, int Score, IReadOnlyList<string> Reasons);

public static partial class BotDetector
{
    public const int BotThreshold = 50;

    private const int HardSignalScore = 100;
    private const string JsBeaconPath = "/api/visits/log";

    private enum OsFamily
    {
        Unknown,
        Windows,
        MacOs,
        Linux,
        Android,
        Ios,
        ChromeOs
    }

    public static bool IsBot(string? userAgent, bool webdriver, string? requestedUrl)
        => Evaluate(new BotDetectionContext
        {
            UserAgent = userAgent ?? string.Empty,
            Webdriver = webdriver,
            RequestedUrl = requestedUrl ?? string.Empty
        }).IsBot;

    public static bool IsBot(RequestLog log) => Evaluate(log).IsBot;

    public static bool IsBot(BotDetectionContext context) => Evaluate(context).IsBot;

    public static BotDetectionResult Evaluate(RequestLog log)
        => Evaluate(BotDetectionContext.FromRequestLog(log));

    public static BotDetectionResult Evaluate(BotDetectionContext context)
    {
        var score = 0;
        var reasons = new List<string>();

        void AddSignal(int points, string reason)
        {
            score += points;
            reasons.Add(reason);
        }

        if (context.Webdriver)
            AddSignal(HardSignalScore, "navigator.webdriver is true");

        if (string.IsNullOrWhiteSpace(context.UserAgent))
            AddSignal(HardSignalScore, "User-Agent is empty");
        else if (BotUserAgentRegex().IsMatch(context.UserAgent))
            AddSignal(HardSignalScore, "User-Agent matches known bot or automation tool");

        if (!string.IsNullOrWhiteSpace(context.RequestedUrl) && ScannerUrlRegex().IsMatch(context.RequestedUrl))
            AddSignal(HardSignalScore, "Requested URL matches scanner probe pattern");

        var isJsBeacon = IsJsBeaconRequest(context);
        var headers = ParseHeaders(context.Extras);

        EvaluateHeaderSignals(context, headers, isJsBeacon, AddSignal);
        EvaluateConsistencySignals(context, isJsBeacon, AddSignal);

        return new BotDetectionResult(score >= BotThreshold, score, reasons);
    }

    private static void EvaluateHeaderSignals(
        BotDetectionContext context,
        Dictionary<string, string>? headers,
        bool isJsBeacon,
        Action<int, string> addSignal)
    {
        if (headers is null || headers.Count == 0)
            return;

        if (headers.TryGetValue("User-Agent", out var httpUserAgent) && !string.IsNullOrWhiteSpace(httpUserAgent))
        {
            if (BotUserAgentRegex().IsMatch(httpUserAgent))
                addSignal(HardSignalScore, "HTTP User-Agent header matches known bot or automation tool");

            if (isJsBeacon &&
                !string.IsNullOrWhiteSpace(context.UserAgent) &&
                !string.Equals(httpUserAgent.Trim(), context.UserAgent.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                addSignal(50, "JS navigator.userAgent differs from HTTP User-Agent header");
            }
        }

        // Only evaluate missing browser headers when Extras clearly holds captured HTTP headers (User-Agent present)
        if (!headers.ContainsKey("User-Agent"))
            return;

        var isHttps = IsHttpsRequest(context.RequestedUrl, headers);
        var claimsModernBrowser = ClaimsModernBrowser(context.UserAgent);

        if (isHttps && claimsModernBrowser && !HasAnyHeader(headers, "Sec-Fetch-Site", "Sec-Fetch-Mode", "Sec-Fetch-Dest"))
            addSignal(25, "Missing Sec-Fetch-* headers on HTTPS modern browser User-Agent");

        if (isHttps && ClaimsModernDesktopOrAndroidChromium(context.UserAgent) && !headers.ContainsKey("Sec-CH-UA"))
            addSignal(20, "Missing Sec-CH-UA header on HTTPS Chromium User-Agent");

        if (claimsModernBrowser &&
            !HasNonEmptyHeader(headers, "Accept-Language") &&
            string.IsNullOrWhiteSpace(context.AcceptLanguage))
        {
            addSignal(20, "Missing Accept-Language header and language preference");
        }

        if (claimsModernBrowser && !HasNonEmptyHeader(headers, "Accept-Encoding"))
            addSignal(15, "Missing Accept-Encoding header on browser User-Agent");

        if (headers.TryGetValue("Sec-CH-UA-Platform", out var chPlatform))
        {
            var uaOs = ParseOsFromUserAgent(context.UserAgent);
            var chOs = ParseOsFromPlatform(chPlatform.Trim('"', ' '));
            if (uaOs != OsFamily.Unknown && chOs != OsFamily.Unknown && uaOs != chOs)
                addSignal(35, $"Sec-CH-UA-Platform ({chOs}) contradicts User-Agent OS ({uaOs})");
        }
    }

    private static void EvaluateConsistencySignals(
        BotDetectionContext context,
        bool isJsBeacon,
        Action<int, string> addSignal)
    {
        var uaOs = ParseOsFromUserAgent(context.UserAgent);
        var platformOs = ParseOsFromPlatform(context.Platform);
        if (IsOsMismatch(uaOs, platformOs))
            addSignal(35, $"Platform ({context.Platform}) contradicts User-Agent OS ({uaOs})");

        if (!isJsBeacon)
            return;

        if (!context.OnLine)
            addSignal(35, "navigator.onLine is false on active JS beacon request");

        if (IsSuspiciousResolution(context.Resolution))
            addSignal(20, $"Suspicious or empty screen resolution '{context.Resolution}' on JS beacon");

        if (IsMissingOrZeroHardwareConcurrency(context.HardwareConcurrency))
            addSignal(15, "HardwareConcurrency is missing or zero on JS beacon");

        if ((uaOs is OsFamily.Android or OsFamily.Ios) &&
            IsMobilePhoneUserAgent(context.UserAgent) &&
            context.MaxTouchPoints == 0)
        {
            addSignal(20, "Mobile User-Agent reports maxTouchPoints = 0");
        }
    }

    private static bool IsJsBeaconRequest(BotDetectionContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.RequestedUrl) &&
            Uri.TryCreate(context.RequestedUrl, UriKind.Absolute, out var uri) &&
            uri.AbsolutePath.TrimEnd('/').Equals(JsBeaconPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrEmpty(context.Resolution) ||
               !string.IsNullOrEmpty(context.Connection) ||
               !string.IsNullOrEmpty(context.DeviceMemory) ||
               !string.IsNullOrEmpty(context.HardwareConcurrency) ||
               context.MaxTouchPoints >= 0 ||
               context.CookieEnabled ||
               context.OnLine;
    }

    private static Dictionary<string, string>? ParseHeaders(string? extras)
    {
        if (string.IsNullOrWhiteSpace(extras) || extras == "{}")
            return null;

        try
        {
            using var document = JsonDocument.Parse(extras);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                    headers[property.Name] = property.Value.GetString() ?? string.Empty;
            }

            return headers;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsHttpsRequest(string requestedUrl, Dictionary<string, string> headers)
    {
        if (requestedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return true;

        return headers.TryGetValue("X-Forwarded-Proto", out var proto) &&
               proto.Equals("https", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAnyHeader(Dictionary<string, string> headers, params string[] names)
    {
        foreach (var name in names)
        {
            if (HasNonEmptyHeader(headers, name))
                return true;
        }

        return false;
    }

    private static bool HasNonEmptyHeader(Dictionary<string, string> headers, string name)
        => headers.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value);

    private static bool ClaimsModernBrowser(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return false;

        if (ChromeVersionRegex().Match(userAgent) is { Success: true } chromeMatch &&
            int.TryParse(chromeMatch.Groups[1].ValueSpan, out var chromeMajor) &&
            chromeMajor >= 90)
        {
            return true;
        }

        if (FirefoxVersionRegex().Match(userAgent) is { Success: true } firefoxMatch &&
            int.TryParse(firefoxMatch.Groups[1].ValueSpan, out var firefoxMajor) &&
            firefoxMajor >= 95)
        {
            return true;
        }

        if (SafariVersionRegex().Match(userAgent) is { Success: true } safariMatch &&
            int.TryParse(safariMatch.Groups[1].ValueSpan, out var safariMajor) &&
            safariMajor >= 17)
        {
            return true;
        }

        return false;
    }

    private static bool ClaimsModernDesktopOrAndroidChromium(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return false;

        // Exclude Firefox, iOS browsers (WebKit), and Android WebView which may omit Client Hints
        if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("CriOS/", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("FxiOS/", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("EdgiOS/", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("; wv)", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return ChromeVersionRegex().Match(userAgent) is { Success: true } match &&
               int.TryParse(match.Groups[1].ValueSpan, out var major) &&
               major >= 95;
    }

    private static OsFamily ParseOsFromUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return OsFamily.Unknown;

        // Check Android and iOS before Linux/Mac because their UAs embed "Linux" and "Mac OS X"
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return OsFamily.Android;

        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPod", StringComparison.OrdinalIgnoreCase))
            return OsFamily.Ios;

        if (userAgent.Contains("Windows NT", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Win64", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Win32", StringComparison.OrdinalIgnoreCase))
            return OsFamily.Windows;

        if (userAgent.Contains("CrOS", StringComparison.OrdinalIgnoreCase))
            return OsFamily.ChromeOs;

        if (userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase))
            return OsFamily.MacOs;

        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("X11", StringComparison.OrdinalIgnoreCase))
            return OsFamily.Linux;

        return OsFamily.Unknown;
    }

    private static OsFamily ParseOsFromPlatform(string platform)
    {
        if (string.IsNullOrWhiteSpace(platform) ||
            platform.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            return OsFamily.Unknown;

        if (platform.Contains("Win", StringComparison.OrdinalIgnoreCase))
            return OsFamily.Windows;

        if (platform.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            platform.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            platform.Contains("iPod", StringComparison.OrdinalIgnoreCase) ||
            platform.Equals("iOS", StringComparison.OrdinalIgnoreCase))
            return OsFamily.Ios;

        if (platform.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return OsFamily.Android;

        if (platform.Contains("Chrome OS", StringComparison.OrdinalIgnoreCase) ||
            platform.Contains("Chromium OS", StringComparison.OrdinalIgnoreCase) ||
            platform.Contains("CrOS", StringComparison.OrdinalIgnoreCase))
            return OsFamily.ChromeOs;

        if (platform.Contains("Mac", StringComparison.OrdinalIgnoreCase))
            return OsFamily.MacOs;

        if (platform.Contains("Linux", StringComparison.OrdinalIgnoreCase) ||
            platform.Contains("X11", StringComparison.OrdinalIgnoreCase))
            return OsFamily.Linux;

        return OsFamily.Unknown;
    }

    private static bool IsOsMismatch(OsFamily uaOs, OsFamily platformOs)
    {
        if (uaOs == OsFamily.Unknown || platformOs == OsFamily.Unknown || uaOs == platformOs)
            return false;

        // Android browsers legitimately report navigator.platform as "Linux armv8l" / "Linux aarch64"
        if (uaOs == OsFamily.Android && platformOs == OsFamily.Linux)
            return false;

        // ChromeOS may report navigator.platform as "Linux x86_64"
        if (uaOs == OsFamily.ChromeOs && platformOs == OsFamily.Linux)
            return false;

        return true;
    }

    private static bool IsSuspiciousResolution(string resolution)
        => string.IsNullOrWhiteSpace(resolution) ||
           resolution.Equals("0x0", StringComparison.OrdinalIgnoreCase) ||
           resolution.Equals("800x600", StringComparison.OrdinalIgnoreCase);

    private static bool IsMissingOrZeroHardwareConcurrency(string hardwareConcurrency)
        => string.IsNullOrWhiteSpace(hardwareConcurrency) ||
           hardwareConcurrency.Trim() == "0";

    private static bool IsMobilePhoneUserAgent(string userAgent)
        => userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
           (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) &&
            userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(
        @"(?:bot\b|crawl|spider|slurp|tracker|ahrefs|semrush|dotbot|rogerbot|mj12|blexbot|bytespider|petalbot|screaming\s*frog|zoominfobot|dataforseo|censys|shodan|curl/|libcurl|wget/|python-requests|aiohttp|httpx|python-urllib|pycurl|scrapy|mechanize|go-http-client|postman|insomnia|httpie|apache-httpclient|okhttp|java/|node-fetch|axios/|undici|libwww-perl|guzzlehttp|dalvik|restsharp|faraday|headlesschrome|phantomjs|selenium|puppeteer|playwright|cypress|zgrab|nmap|masscan|nikto|sqlmap|openvas|qualys|nuclei|gobuster|ffuf|feroxbuster|wpscan|wappalyzer|uptime-kuma|checkly|pingdom|statuscake|facebookexternalhit|meta-externalagent|linkedinbot|slackbot|telegrambot|discordbot|whatsapp|pinterest|google-inspectiontool|google-extended|chatgpt-user|anthropic-ai|cohere-ai|perplexity-user|mistralai-user|diffbot|yandex|baidu)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BotUserAgentRegex();

    [GeneratedRegex(
        @"(?:/wp-(?:admin|includes|content|login|json)|/xmlrpc\.php|/phpmyadmin|/pma|/actuator|/solr|/boaform|/cgi-bin/|/vendor/|/phpunit|/_profiler|/telescope|/server-status|/etc/passwd|/\.(?:env|git|svn|htaccess|htpasswd|aws|ssh|ds_store|vscode)(?:/|$)|(?:\.\./|\.\.\\|%2e%2e)|(?:\.php|\.asp|\.aspx|\.jsp|\.cgi|\.bak|\.sql|\.tar|\.gz|\.zip|\.rar|\.7z)(?:\?|$))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ScannerUrlRegex();

    [GeneratedRegex(@"Chrome/(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ChromeVersionRegex();

    [GeneratedRegex(@"Firefox/(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FirefoxVersionRegex();

    [GeneratedRegex(@"Version/(\d+)(?:\.\d+)*\s+(?:Mobile/\S+\s+)?Safari/", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SafariVersionRegex();
}
