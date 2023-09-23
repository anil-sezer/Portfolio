using Portfolio.WebUi.Services.BingBackground;
using Serilog;

namespace Portfolio.WebUi.Services;

public static class BackgroundImageFromBingService
{
    public static string GetBackgroundImg(string imgFolderPath)
    {
        const string imgFilePrefix = "bingImgOfTheDay_";
        var fileName = $"{imgFolderPath}/{imgFilePrefix}{DateTime.Now:dd-MM-yyyy}.bmp";

        var fileInfo = new FileInfo(fileName);
        if (fileInfo.Exists)
            return ReturnImageAddressFromRoot(fileName);
        
        
        RemoveOlderBingImages(Directory.GetFiles(imgFolderPath, $"*{imgFilePrefix}*")); // todo: Use async for this!
        return ReturnImageAddressFromRoot(DownloadImage.DownloadAndGiveImgRouteAsync(GetResolution(), fileName).Result.FullName);
    }

    private static string ReturnImageAddressFromRoot(string absoluteAddress)
    {
        return absoluteAddress.Split("wwwroot")[1].Replace('\\', '/');
    }

    private static void RemoveOlderBingImages(string[] matchingFiles)
    {
        foreach (var filePath in matchingFiles)
        {
            File.Delete(filePath);
            Log.Information("Deleted: {FilePath}", filePath);
        }
    } 

    // todo: Find a way to get resolution before page load
    private static string GetResolution()
    {
        return "_1920x1080.jpg";
    }
}
