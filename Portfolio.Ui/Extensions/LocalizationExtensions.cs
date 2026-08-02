using System.Globalization;
using Microsoft.AspNetCore.Localization;

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

            app.UseRequestLocalization(localizationOptions);
        }

        public void DefineSetCultureCookieRoute()
        {
            app.MapGet("/SetCulture", (string culture, string? redirectUri, HttpContext httpContext) =>
            {
                if (!string.IsNullOrWhiteSpace(culture))
                {
                    httpContext.Response.Cookies.Append(
                        CookieRequestCultureProvider.DefaultCookieName,
                        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax }
                    );
                }
                return Results.LocalRedirect(string.IsNullOrWhiteSpace(redirectUri) ? "/" : redirectUri);
            });
        }
    }
}
