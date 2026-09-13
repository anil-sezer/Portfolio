using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Portfolio.Ui.Localization;

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
        }

        public void DefineEnglishRedirectRoute()
        {
            app.MapGet("/en", () => Results.Redirect("/", permanent: true));
        }
    }
}
