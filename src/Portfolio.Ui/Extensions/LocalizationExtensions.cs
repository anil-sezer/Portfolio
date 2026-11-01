using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Portfolio.Ui.Localization;
using Portfolio.Ui.Middlewares;

namespace Portfolio.Ui.Extensions;

public static class LocalizationExtensions
{
    extension(WebApplication app)
    {
        public void SetupLocalization()
        {
            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("en"),
                new CultureInfo("tr-TR"),
                new CultureInfo("tr")
            };

            var localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            };

            // Use URL-based culture detection instead of cookies for SEO
            localizationOptions.RequestCultureProviders.Clear();
            localizationOptions.RequestCultureProviders.Add(new UrlSegmentCultureProvider());

            app.UseRequestLocalization(localizationOptions);
            app.UseMiddleware<LanguageRedirectMiddleware>();
        }

        public void DefineEnglishRedirectRoute()
        {
            app.MapGet("/en", (HttpContext httpContext) =>
            {
                httpContext.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("en-US")),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        IsEssential = true,
                        SameSite = SameSiteMode.Lax,
                        Path = "/"
                    });

                var target = string.IsNullOrEmpty(httpContext.Request.QueryString.Value)
                    ? "/"
                    : $"/{httpContext.Request.QueryString.Value}";

                return Results.Redirect(target, permanent: false);
            });
        }
    }
}
