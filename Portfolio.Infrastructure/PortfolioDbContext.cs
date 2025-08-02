using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure;

public class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options)
    {
    }

    public DbSet<RequestLog> RequestLog { get; set; }
    public DbSet<NotificationToAdmin> NotificationToAdmin { get; set; }
    public DbSet<ImageOfTheDay> ImageOfTheDay { get; set; }
}
