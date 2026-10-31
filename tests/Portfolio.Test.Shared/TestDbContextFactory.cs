using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Portfolio.Infrastructure;

namespace Portfolio.Test.Shared;

public static class TestDbContextFactory
{
    public static DbContextOptions<PortfolioDbContext> CreateOptions(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString("N");
        return new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    public static PortfolioDbContext Create(string? dbName = null)
    {
        var options = CreateOptions(dbName);
        return new PortfolioDbContext(options);
    }
}
