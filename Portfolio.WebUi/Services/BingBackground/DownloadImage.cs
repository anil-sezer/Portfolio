using System.Net;
using Newtonsoft.Json;

namespace Portfolio.WebUi.Services.BingBackground;

public static class DownloadImage
{
    public static async Task<FileInfo> DownloadAndGiveImgRouteAsync(string resolution, string savePath)
    {
        var urlBase = GetBackgroundUrlBase();
        var backgroundBytes = await DownloadBackgroundAsync(urlBase + resolution);
        var img = Image.Load(backgroundBytes);
        
        return SaveBackgroundImage(img, savePath);
    }
    
    private static string GetBackgroundUrlBase() {
        var jsonObject = DownloadJson();
        return "https://www.bing.com" + jsonObject.images[0].urlbase;
    }
    
    // todo: obselete, update
    // todo: Can I somehow get users locale?
    private static dynamic DownloadJson()
    {
        const string locale = "en-US";
        using WebClient webClient = new WebClient();
        Console.WriteLine("Downloading JSON...");
        var jsonString = webClient.DownloadString("https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=" + locale);
        return JsonConvert.DeserializeObject<dynamic>(jsonString);
    }
    
    private static async Task<byte[]> DownloadBackgroundAsync(string url)
    {
        Console.WriteLine("Downloading background...");
        using HttpClient httpClient = new HttpClient();
        
        return await httpClient.GetByteArrayAsync(url);
    }
    
    private static FileInfo SaveBackgroundImage(Image img, string savePath)
    {
        Console.WriteLine("Saving background img...");
        
        using var outputStream = new FileStream(savePath, FileMode.Create);
        img.SaveAsBmp(outputStream);

        return new FileInfo(savePath);
    }
}
