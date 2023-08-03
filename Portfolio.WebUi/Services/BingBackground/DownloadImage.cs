using System.Net;
using Newtonsoft.Json;

namespace Portfolio.WebUi.Services.BingBackground;

public static class DownloadImage
{
    public static async Task<string> DownloadAndGiveImgRouteAsync(string resolution, string savePath)
    {
        var urlBase = GetBackgroundUrlBase();
        var backgroundBytes = await DownloadBackgroundAsync(urlBase + resolution);
        var img = Image.Load(backgroundBytes);
        
        SaveBackgroundImage(img, savePath);

        return "";
    }
    
    private static string GetBackgroundUrlBase() {
        var jsonObject = DownloadJson();
        return "https://www.bing.com" + jsonObject.images[0].urlbase;
    }
    
    // todo: obselete, update
    // todo: Can I somehow get users locale?
    private static dynamic DownloadJson()
    {
        var locale = "en-US";
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
    
    private static void SaveBackgroundImage(Image img, string savePath)
    {
        Console.WriteLine("Saving background img...");
        
        using var outputStream = new FileStream(savePath, FileMode.Create);
        img.SaveAsBmp(outputStream); // todo: Dosya ismini ve .bmp'yi ekle
    }

    // private static string GetBackgroundImagePath() {
    //     var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Bing Backgrounds", DateTime.Now.Year.ToString());
    //     Directory.CreateDirectory(directory);
    //     return Path.Combine(directory, DateTime.Now.ToString("M-d-yyyy") + ".bmp");
    // }
}
