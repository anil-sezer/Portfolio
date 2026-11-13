using FluentAssertions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Utilities;

namespace Portfolio.Test.Domain;

public class BotDetectorTests
{
    [Fact]
    public void IsBot_WhenWebdriverIsTrue_ReturnsTrue()
    {
        // Act
        var result = BotDetector.IsBot("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", webdriver: true, "https://anilsezer.com/");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsBot_WhenUserAgentIsNullOrWhitespace_ReturnsTrue(string? userAgent)
    {
        // Act
        var result = BotDetector.IsBot(userAgent, webdriver: false, "https://anilsezer.com/");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    // Search engine bots
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)")]
    [InlineData("Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)")]
    [InlineData("Mozilla/5.0 (compatible; Yahoo! Slurp; http://help.yahoo.com/help/us/ysearch/slurp)")]
    [InlineData("Mozilla/5.0 (compatible; DuckDuckBot-Https/1.1; https://duckduckgo.com/duckduckbot)")]
    [InlineData("Mozilla/5.0 (compatible; Baiduspider/2.0; +http://www.baidu.com/search/spider.html)")]
    [InlineData("Mozilla/5.0 (compatible; YandexBot/3.0; +http://yandex.com/bots)")]
    [InlineData("Google-InspectionTool/1.0")]
    // SEO & crawler bots
    [InlineData("Mozilla/5.0 (compatible; AhrefsBot/7.0; +http://ahrefs.com/robot/)")]
    [InlineData("Mozilla/5.0 (compatible; SemrushBot/7~bl; +http://www.semrush.com/bot.html)")]
    [InlineData("Mozilla/5.0 (compatible; DotBot/1.2; +https://opensiteexplorer.org/dotbot)")]
    [InlineData("Mozilla/5.0 (compatible; MJ12bot/v1.4.8; http://mj12bot.com/)")]
    [InlineData("ByteSpider; https://bytespider.byteplus.com/faq")]
    [InlineData("PetalBot; +https://aspiegel.com/petalbot")]
    [InlineData("Screaming Frog SEO Spider/19.0")]
    // AI crawlers
    [InlineData("Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; ChatGPT-User/1.0; +https://openai.com/bot)")]
    [InlineData("Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; anthropic-ai; +https://www.anthropic.com)")]
    [InlineData("Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; ClaudeBot/1.0; +claudebot@anthropic.com)")]
    [InlineData("Mozilla/5.0 (compatible; PerplexityBot/1.0; +https://perplexity.ai/perplexitybot)")]
    // CLI / Scripting tools
    [InlineData("curl/8.5.0")]
    [InlineData("Wget/1.21.4")]
    [InlineData("python-requests/2.31.0")]
    [InlineData("Python/3.11 aiohttp/3.9.1")]
    [InlineData("python-urllib/3.10")]
    [InlineData("Go-http-client/1.1")]
    [InlineData("PostmanRuntime/7.36.0")]
    [InlineData("HTTPie/3.2.2")]
    [InlineData("Apache-HttpClient/4.5.14 (Java/17.0.9)")]
    [InlineData("okhttp/4.12.0")]
    [InlineData("node-fetch/3.3.2")]
    [InlineData("axios/1.6.5")]
    // Headless / Testing
    [InlineData("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/120.0.6099.109 Safari/537.36")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Cypress/13.6.2")]
    [InlineData("Selenium/4.16.1")]
    [InlineData("Playwright/1.40.0")]
    // Scanners & Probes
    [InlineData("Mozilla/5.0 (compatible; CensysInspect/1.1; +https://about.censys.io/)")]
    [InlineData("zgrab/0.x")]
    [InlineData("sqlmap/1.7.12#stable")]
    // Social media scrapers
    [InlineData("facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)")]
    [InlineData("LinkedInBot/1.0 (compatible; Mozilla/5.0; Apache-HttpClient +http://www.linkedin.com)")]
    [InlineData("Twitterbot/1.0")]
    [InlineData("Slackbot-LinkExpanding 1.0 (+https://api.slack.com/robots)")]
    [InlineData("TelegramBot (like TwitterBot)")]
    [InlineData("Mozilla/5.0 (compatible; Discordbot/2.0; +https://discordapp.com)")]
    public void IsBot_WhenUserAgentIsKnownBotOrTool_ReturnsTrue(string userAgent)
    {
        // Act
        var result = BotDetector.IsBot(userAgent, webdriver: false, "https://anilsezer.com/");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:132.0) Gecko/20100101 Firefox/132.0")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0")]
    [InlineData("Mozilla/5.0 (Linux; Android 13; SAMSUNG SM-A536B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/23.0 Chrome/115.0.5790.166 Mobile Safari/537.36")]
    public void IsBot_WhenUserAgentIsLegitimateBrowser_ReturnsFalse(string userAgent)
    {
        // Act
        var result = BotDetector.IsBot(userAgent, webdriver: false, "https://anilsezer.com/");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("https://anilsezer.com/.env")]
    [InlineData("https://anilsezer.com/wp-login.php")]
    [InlineData("https://anilsezer.com/wp-admin/index.php")]
    [InlineData("https://anilsezer.com/wp-includes/wlwmanifest.xml")]
    [InlineData("https://anilsezer.com/xmlrpc.php")]
    [InlineData("https://anilsezer.com/phpmyadmin/")]
    [InlineData("https://anilsezer.com/pma/")]
    [InlineData("https://anilsezer.com/actuator/health")]
    [InlineData("https://anilsezer.com/cgi-bin/test.cgi")]
    [InlineData("https://anilsezer.com/.git/config")]
    [InlineData("https://anilsezer.com/config.bak")]
    [InlineData("https://anilsezer.com/backup.sql")]
    public void IsBot_WhenRequestedUrlIsScannerProbe_ReturnsTrue(string requestedUrl)
    {
        // Arrange - even with legitimate browser UA, scanner paths indicate an exploit probe
        const string legitimateUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";

        // Act
        var result = BotDetector.IsBot(legitimateUserAgent, webdriver: false, requestedUrl);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://anilsezer.com/")]
    [InlineData("https://anilsezer.com/resume")]
    [InlineData("https://anilsezer.com/projects")]
    [InlineData("https://anilsezer.com/contact")]
    [InlineData("https://anilsezer.com/non-existent-page")]
    [InlineData("https://anilsezer.com/resume?lang=en")]
    public void IsBot_WhenRequestedUrlIsNormal_ReturnsFalse(string requestedUrl)
    {
        // Arrange
        const string legitimateUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";

        // Act
        var result = BotDetector.IsBot(legitimateUserAgent, webdriver: false, requestedUrl);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsBot_WithRequestLogOverload_EvaluatesCorrectly()
    {
        // Arrange
        var humanLog = new RequestLog
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",
            AcceptLanguage = "en-US",
            Platform = "Win32",
            Webdriver = false,
            DeviceMemory = "8",
            HardwareConcurrency = "8",
            MaxTouchPoints = 0,
            DoNotTrack = "1",
            Connection = "wifi",
            CookieEnabled = true,
            OnLine = true,
            Referrer = "https://google.com",
            Resolution = "1920x1080",
            ClientIp = "198.51.100.1",
            Country = "DefaultCountry",
            City = "DefaultCity",
            RequestedUrl = "https://anilsezer.net/resume",
            Extras = "{}"
        };

        var botLog = new RequestLog
        {
            UserAgent = "curl/8.5.0",
            AcceptLanguage = "",
            Platform = "",
            Webdriver = false,
            DeviceMemory = "",
            HardwareConcurrency = "",
            MaxTouchPoints = -1,
            DoNotTrack = "",
            Connection = "",
            CookieEnabled = false,
            OnLine = false,
            Referrer = "",
            Resolution = "",
            ClientIp = "198.51.100.2",
            Country = "DefaultCountry",
            City = "DefaultCity",
            RequestedUrl = "https://anilsezer.net/",
            Extras = "{}"
        };

        // Act & Assert
        BotDetector.IsBot(humanLog).Should().BeFalse();
        BotDetector.IsBot(botLog).Should().BeTrue();
    }
}
