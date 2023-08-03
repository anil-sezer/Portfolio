using Portfolio.WebUi.Services.BingBackground;

namespace Portfolio.WebUi.Services;

public static class BackgroundImageFromBingService
{
    public static async Task<string> GetBackgroundImg(string savePath)
    {
        
        var route = await DownloadImage.DownloadAndGiveImgRouteAsync(GetResolution(), savePath);

        return route;
    }

    // todo: Find a way to get resolution before page load
    private static string GetResolution()
    {
        return "_1920x1080.jpg";
    }
}
