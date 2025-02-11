using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces.Repositories;
using Portfolio.Domain.Interfaces.Repositories.Dtos;

namespace Portfolio.Infrastructure.Repositories;

public class EmailToAdminRepository(PortfolioDbContext dbContext) : BaseRepository<EmailToAdmin>(dbContext), IEmailToAdminRepository
{
    public async Task<bool> IsThisEmailAlreadySentAtLastHourAsync(EmailDto dto)
    {
        return await dbContext.EmailToAdmin
            .AnyAsync(x => x.Name == dto.Name &&
                           x.EmailAddress == dto.EmailAddress &&
                           x.Subject == dto.Subject &&
                           x.Message == dto.Message &&
                           x.CreatedAt > DateTime.UtcNow.AddHours(-1));
    }
}
