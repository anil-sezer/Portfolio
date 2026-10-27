using Microsoft.EntityFrameworkCore;
using Portfolio.Infrastructure;

namespace Portfolio.Grpc.Services;

public partial class BackgroundImageServices(PortfolioDbContext dbContext, HttpClient httpClient, ILogger<BackgroundImageServices> logger) : BackgroundImages.BackgroundImagesBase
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Serving this background image: {ImageUrl}")]
    private static partial void LogServingBackgroundImage(ILogger logger, string imageUrl);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Cannot get a background image from the database. Frontend will decide what to serve.")]
    private static partial void LogMissingBackgroundImage(ILogger logger);

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
            .AsNoTracking()
            .Where(x => x.UrlWorks && x.DoIPreferToDisplayThis)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (img != null)
        {
            LogServingBackgroundImage(logger, img.ImageUrl);
            return img;
        }

        LogMissingBackgroundImage(logger);
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
