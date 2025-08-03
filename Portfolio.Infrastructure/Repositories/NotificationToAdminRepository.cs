using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces.Repositories;
using Portfolio.Domain.Interfaces.Repositories.Dtos;

namespace Portfolio.Infrastructure.Repositories;

public class NotificationToAdminRepository(PortfolioDbContext dbContext) : BaseRepository<NotificationToAdmin>(dbContext), INotificationToAdminRepository
{
    public async Task<bool> IsThisEmailAlreadySentAtLastHourAsync(NotificationDto dto)
    {
        return await dbContext.NotificationsToAdmin
            .AnyAsync(x => x.Name == dto.Name &&
                           x.EmailAddress == dto.EmailAddress &&
                           x.Subject == dto.Subject &&
                           x.Message == dto.Message &&
                           x.CreatedAt > DateTime.UtcNow.AddHours(-1));
    }
}
