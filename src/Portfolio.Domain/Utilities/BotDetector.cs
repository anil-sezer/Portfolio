using System.Text.RegularExpressions;
using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Utilities;

public static partial class BotDetector
{
    public static bool IsBot(string? userAgent, bool webdriver, string? requestedUrl)
    {
        if (webdriver)
            return true;

        if (string.IsNullOrWhiteSpace(userAgent))
            return true;

        if (BotUserAgentRegex().IsMatch(userAgent))
            return true;

        if (!string.IsNullOrWhiteSpace(requestedUrl) && ScannerUrlRegex().IsMatch(requestedUrl))
            return true;

        return false;
    }

    public static bool IsBot(RequestLog log) => IsBot(log.UserAgent, log.Webdriver, log.RequestedUrl);

    [GeneratedRegex(
        @"(?:bot\b|crawl|spider|slurp|tracker|ahrefs|semrush|dotbot|rogerbot|mj12|blexbot|bytespider|petalbot|screaming\s*frog|zoominfobot|dataforseo|censys|shodan|curl/|wget/|python-requests|aiohttp|httpx|python-urllib|go-http-client|postman|insomnia|httpie|apache-httpclient|okhttp|java/|node-fetch|axios/|libwww-perl|guzzlehttp|headlesschrome|phantomjs|selenium|puppeteer|playwright|cypress|zgrab|nmap|masscan|nikto|sqlmap|openvas|qualys|facebookexternalhit|meta-externalagent|linkedinbot|slackbot|telegrambot|discordbot|whatsapp|pinterest|google-inspectiontool|chatgpt-user|anthropic-ai|cohere-ai|yandex|baidu)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BotUserAgentRegex();

    [GeneratedRegex(
        @"(?:/wp-(?:admin|includes|content|login)|/xmlrpc\.php|/phpmyadmin|/pma|/actuator|/solr|/boaform|/cgi-bin/|/\.(?:env|git|svn|htaccess|htpasswd|aws|ssh)(?:/|$)|(?:\.php|\.asp|\.aspx|\.jsp|\.cgi|\.bak|\.sql|\.tar|\.gz|\.zip|\.rar|\.7z)(?:\?|$))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ScannerUrlRegex();
}
