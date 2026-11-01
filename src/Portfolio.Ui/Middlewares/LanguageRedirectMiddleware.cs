using Microsoft.AspNetCore.Localization;
using Microsoft.Net.Http.Headers;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace Portfolio.Ui.Middlewares;

public class LanguageRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // If visiting /tr, ensure the Turkish culture cookie is set for future visits to /
        if (path.StartsWith("/tr", StringComparison.OrdinalIgnoreCase) &&
            (path.Length == 3 || path[3] == '/'))
        {
            EnsureCultureCookie(context, "tr-TR");
            await next(context);
            return;
        }

        // Only redirect root GET/HEAD requests
        if ((HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)) &&
            (path == "/" || path == string.Empty))
        {
            if (ShouldRedirectToTurkish(context))
            {
                var target = string.IsNullOrEmpty(context.Request.QueryString.Value)
                    ? "/tr"
                    : $"/tr{context.Request.QueryString.Value}";

                context.Response.Redirect(target, permanent: false);
                return;
            }
        }

        await next(context);
    }

    public static bool ShouldRedirectToTurkish(HttpContext context)
    {
        // 1. Check if user has an explicit culture preference cookie
        if (context.Request.Cookies.TryGetValue(CookieRequestCultureProvider.DefaultCookieName, out var cookieValue) &&
            !string.IsNullOrWhiteSpace(cookieValue))
        {
            var result = CookieRequestCultureProvider.ParseCookieValue(cookieValue);
            if (result != null && result.Cultures.Count > 0)
            {
                if (result.Cultures.Any(c => c.Value != null && c.Value.StartsWith("tr", StringComparison.OrdinalIgnoreCase)))
                    return true;

                if (result.Cultures.Any(c => c.Value != null && c.Value.StartsWith("en", StringComparison.OrdinalIgnoreCase)))
                    return false;
            }
        }

        // 2. If no explicit cookie is present, check Accept-Language header
        var acceptLanguage = context.Request.Headers.AcceptLanguage.ToString();
        return PrefersTurkish(acceptLanguage);
    }

    public static bool PrefersTurkish(string? acceptLanguageHeader)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguageHeader))
            return false;

        if (!StringWithQualityHeaderValue.TryParseList(new[] { acceptLanguageHeader }, out var languages) || languages.Count == 0)
            return false;

        foreach (var lang in languages.Where(l => (l.Quality ?? 1.0) > 0).OrderByDescending(l => l.Quality ?? 1.0))
        {
            var value = lang.Value.Value;
            if (string.IsNullOrEmpty(value))
                continue;

            if (value.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
                return true;

            if (value.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return false;
    }

    private static void EnsureCultureCookie(HttpContext context, string culture)
    {
        if (context.Request.Cookies.TryGetValue(CookieRequestCultureProvider.DefaultCookieName, out var cookieVal) &&
            !string.IsNullOrEmpty(cookieVal))
        {
            var result = CookieRequestCultureProvider.ParseCookieValue(cookieVal);
            if (result != null && result.Cultures.Any(c => c.Value != null && c.Value.StartsWith(culture, StringComparison.OrdinalIgnoreCase)))
                return;
        }

        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });
    }
}
