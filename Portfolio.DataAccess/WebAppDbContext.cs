using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities.WebAppEntities;

namespace Portfolio.DataAccess;

public class WebAppDbContext : DbContext
{
    public WebAppDbContext(DbContextOptions<WebAppDbContext> options) : base(options)
    {
        // _httpContextAccessor = httpContextAccessor;
    }
    public DbSet<Request> Request { get; set; }
    public DbSet<BingDailyBackground> BingDailyBackground { get; set; }
    public DbSet<Email> Email { get; set; }
    public DbSet<IotDirective> IotDirective { get; set; } // todo-anil-beforeMerge: Create another DBContext for the webApi project
    public DbSet<IotReport> IotReport { get; set; } // todo-anil-beforeMerge: Create another DBContext for the webApi project
}