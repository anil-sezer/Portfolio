using System.Net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Portfolio.DataAccess;
using Portfolio.Domain.Constants;
using Portfolio.Domain.Entities.WebAppEntities;
using Serilog;

namespace Portfolio.WebUi.Services;

public class BackgroundImageFromBingService
{
    private readonly WebAppDbContext _dbContext;

    public BackgroundImageFromBingService(WebAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GetBackgroundImg()
    {
        var img = await _dbContext.BingDailyBackground.SingleOrDefaultAsync(x => x.CreationTime.Date == DateTime.Now.Date);

        if (img != null)
        {
            if (await IsValidImageUrlAsync(img.ImageUrl))
                return img.ImageUrl;

            img.UrlWorks = false;
            _dbContext.SaveChangesAsync();
            return Etcetera.DefaultBackgroundImage;
        }
        
        var url = await GetDailyImageUrlAsync();

        var bg = new BingDailyBackground
        {
            ImageUrl = url,
            UrlWorks = await IsValidImageUrlAsync(url)
        };
        await _dbContext.AddAsync(bg);
        _dbContext.SaveChangesAsync();

        return bg.UrlWorks ? bg.ImageUrl : Etcetera.DefaultBackgroundImage;
    }
    
    private async Task<bool> IsValidImageUrlAsync(string imageUrl)
    {
        using var httpClient = new HttpClient();
        
        try
        {
            var response = await httpClient.GetAsync(imageUrl);

            if (response.StatusCode != HttpStatusCode.OK) 
                return false;
            
            // Verify that the Content-Type is an image:
            if (response.Content.Headers.ContentType != null && 
                response.Content.Headers.ContentType.MediaType.StartsWith("image/"))
            {
                return true;
            }

            Log.Error("Background image Url is not valid: {ImageUrl}, HttpStatusCode: {StatusCode}", imageUrl, response.StatusCode);
            return false;
        }
        catch
        {
            Log.Error("Background image Url is not valid: {ImageUrl}", imageUrl);
            return false;
        }
    }

    private static async Task<string> GetDailyImageUrlAsync() {
        var jsonObject = await DownloadJsonAsync();
        var urlBase =  "https://www.bing.com" + jsonObject.images[0].urlbase;

        return urlBase + "_1920x1080.jpg";
    }
    
    private static async Task<dynamic> DownloadJsonAsync()
    {
        const string locale = "tr-TR";
        const string url = $"https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt={locale}";

        using var httpClient = new HttpClient();
    
        Log.Information("Downloading JSON...");
        var json = await httpClient.GetStringAsync(url);
        Log.Information("Json string: {Json}", json);


        return JsonConvert.DeserializeObject<dynamic>(json);
    }
}
