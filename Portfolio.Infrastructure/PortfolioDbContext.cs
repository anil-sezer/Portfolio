using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure;

public class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options)
    {
    }

    public DbSet<RequestLog> RequestLogs { get; set; }
    public DbSet<NotificationToAdmin> NotificationsToAdmin { get; set; }
    public DbSet<DailyImage> DailyImages { get; set; }
}
