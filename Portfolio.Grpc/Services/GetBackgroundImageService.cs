using Portfolio.Domain.Interfaces.Repositories;

namespace Portfolio.Grpc.Services;

public class GetBackgroundImageService(IImageOfTheDayRepository repository) : BackgroundImages.BackgroundImagesBase
{
    public override async Task<BackgroundImageDetails> Get(Empty empty, ServerCallContext context)
    {
        var img = await repository.GetLatestBackgroundImageDetailsAsync();
        
        return new BackgroundImageDetails { Url = img.ImageUrl, Source = (ImageOfTheDaySource)img.Source, AltText = img.AltText};
    }
}
