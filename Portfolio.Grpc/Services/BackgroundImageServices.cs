using Portfolio.Domain.Interfaces.Repositories;

namespace Portfolio.Grpc.Services;

public class BackgroundImageServices(IImageOfTheDayRepository repository) : BackgroundImages.BackgroundImagesBase
{
    public override async Task<BackgroundImageDetails> Get(Empty empty, ServerCallContext context)
    {
        var img = await repository.GetLatestBackgroundImageDetailsAsync();
        
        return new BackgroundImageDetails { Url = img.ImageUrl, Source = (ImageOfTheDaySource)img.Source, AltText = img.AltText};
    }

    public override async Task<Empty> Persist(BackgroundImageDetails imgToPersist, ServerCallContext context)
    {
        var urlWorks = await CheckUrlAsync(imgToPersist.Url);

        await repository.CreateAsync(new DailyImage
        {
            AltText = imgToPersist.AltText,
            ImageUrl = imgToPersist.Url,
            Source = (Domain.Enums.ImageOfTheDaySource)imgToPersist.Source,
            DoIPreferToDisplayThis = urlWorks,
            UrlWorks = urlWorks
        });

        return new Empty();
    }
    
    private async Task<bool> CheckUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            // todo: will this work for all sources?
            // Use HEAD request to check if URL is accessible without downloading content
            using var response = await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Head, url));
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
