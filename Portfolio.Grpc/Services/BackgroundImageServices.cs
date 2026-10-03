using Microsoft.EntityFrameworkCore;
using Portfolio.Infrastructure;

namespace Portfolio.Grpc.Services;

public class BackgroundImageServices(PortfolioDbContext dbContext, HttpClient httpClient) : BackgroundImages.BackgroundImagesBase
{
    public override async Task<BackgroundImageDetails> Get(Empty request, ServerCallContext context)
    {
        var img = await GetLatestBackgroundImageDetailsAsync(context.CancellationToken);
        
        return new BackgroundImageDetails { Url = img.ImageUrl, Source = (ImageOfTheDaySource)img.Source, AltText = img.AltText};
    }

    public override async Task<Empty> Persist(BackgroundImageDetails request, ServerCallContext context)
    {
        var urlWorks = await CheckUrlAsync(request.Url, context.CancellationToken);

        dbContext.DailyImages.Add(new DailyImage
        {
            AltText = request.AltText,
            ImageUrl = request.Url,
            Source = (Domain.Enums.ImageOfTheDaySource)request.Source,
            DoIPreferToDisplayThis = urlWorks,
            UrlWorks = urlWorks
        });

        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new Empty();
    }

    private async Task<DailyImage> GetLatestBackgroundImageDetailsAsync(CancellationToken cancellationToken = default)
    {
        var img = await dbContext.DailyImages
            .Where(x => x.UrlWorks && x.DoIPreferToDisplayThis)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (img != null)
        {
            Log.Information("Serving this background image: {ImageUrl}", img.ImageUrl);
            return img;
        }

        Log.Error("Cannot get a background image from the database. Frontend will decide what to serve.");
        return new DailyImage
        {
            ImageUrl = "",
            AltText = "",
            Source = Domain.Enums.ImageOfTheDaySource.None,
            UrlWorks = false,
            DoIPreferToDisplayThis = false
        };
    }
    
    private async Task<bool> CheckUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            // todo: will this work for all sources?
            // Use HEAD request to check if URL is accessible without downloading content
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
