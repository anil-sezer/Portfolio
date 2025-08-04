using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Memory;
using Portfolio.Ui.Models;


namespace Portfolio.Ui.Services;

public class BackgroundImageService(BackgroundImages.BackgroundImagesClient backgroundImagesClient, IMemoryCache memoryCache)
{
    private const string FallbackBackgroundImage = "https://raw.githubusercontent.com/anil-sezer/Portfolio/prod/Portfolio.Ui/wwwroot/img/default-bg.jpg";
    private const string FallbackAltText = "A lush, tropical landscape with a dramatic view of a conical mountain in the distance, possibly a volcano, surrounded by layers of rolling hills and dense forests. The sky above features a stunning mix of colors and clouds.";
    
    private const string CacheKey = "background_image";

    private async Task<BackgroundImageModel> GetFromDbAsync()
    {
        var response = await backgroundImagesClient.GetAsync(new Empty());
        Log.Information("\ud83d\udce8 Sent a gRPC request to {ServiceName}, ResponseUrl: {ResponseUrl}", nameof(backgroundImagesClient.GetAsync), response.Url);

        if (response.Source == ImageOfTheDaySource.None)
        {
            Log.Error("\u274c There is no image of the day available at gRPC, serving default bg img.");
            return new()
            {
                Url = FallbackBackgroundImage,
                AltText = FallbackAltText,
                Source = ImageOfTheDaySource.Bing
            };
        }
            
        return new() {
            Url = response.Url,
            AltText = response.AltText,
            Source = response.Source
        };
    }
    
    public async Task<BackgroundImageModel> GetFromCacheAsync()
    {
        if (memoryCache.TryGetValue(CacheKey, out BackgroundImageModel? cachedImage) && cachedImage != null)
        {
            Log.Information("📦 Background image retrieved from cache");
            return cachedImage;
        }

        var image = await GetFromDbAsync();
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(23)
        };
        
        memoryCache.Set(CacheKey, image, cacheOptions);
        Log.Information("💾 Background image added to cache with 23-hour expiration");
        
        return image;
    }
}
