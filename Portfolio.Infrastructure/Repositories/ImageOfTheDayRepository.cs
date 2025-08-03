using Microsoft.EntityFrameworkCore;
using Portfolio.Infrastructure.Constants;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces.Repositories;
using Serilog;

namespace Portfolio.Infrastructure.Repositories;

public class ImageOfTheDayRepository(PortfolioDbContext dbContext) : BaseRepository<DailyImage>(dbContext), IImageOfTheDayRepository
{
    public async Task<DailyImage> GetLatestBackgroundImageDetailsAsync()
    {
        var img = await dbContext.DailyImages
            .Where(x => x.UrlWorks && x.DoIPreferToDisplayThis)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (img != null)
        {
            Log.Information("Serving this background image: {ImageUrl}", img.ImageUrl);
            return img;
        }

        Log.Error("Cannot get a background image from the database. Default image served");
        return new DailyImage
        {
            ImageUrl = DefaultValues.DefaultBackgroundImage,
            AltText = DefaultValues.DefaultAltText,
            Source = ImageOfTheDaySource.Bing,
            UrlWorks = true,
            DoIPreferToDisplayThis = true
        };
    }
}