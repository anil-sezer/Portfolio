using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces.Repositories.Dtos;

namespace Portfolio.Domain.Interfaces.Repositories;

public interface IEmailToAdminRepository : IRepository<EmailToAdmin>
{
    Task<bool> IsThisEmailAlreadySentAtLastHourAsync(EmailDto dto);
}
