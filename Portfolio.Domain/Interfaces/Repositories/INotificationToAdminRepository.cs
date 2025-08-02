using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces.Repositories.Dtos;

namespace Portfolio.Domain.Interfaces.Repositories;

public interface INotificationToAdminRepository : IRepository<NotificationToAdmin>
{
    Task<bool> IsThisEmailAlreadySentAtLastHourAsync(NotificationDto dto);
}
