using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Interfaces.Repositories;

public interface IImageOfTheDayRepository : IRepository<DailyImage>
{
    Task<DailyImage> GetLatestBackgroundImageDetailsAsync();
}