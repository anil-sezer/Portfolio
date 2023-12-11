using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.DataAccess;

namespace Portfolio.Test.Shared;

public class TestBase
{
    protected readonly WebAppDbContext DefaultTestDbContext;

    public TestBase()
    {
        DefaultTestDbContext = GetDefaultTestDbContext();
    }

    /// <summary>
    /// This method can be used for new scoped (same database) of default dbContext
    /// </summary>
    /// <returns>New scoped instance (same database) of default dbContext</returns>
    protected WebAppDbContext GetDefaultTestDbContext()
    {
        var provider = GetNewHostServiceProvider().CreateScope().ServiceProvider;

        return provider.GetRequiredService<WebAppDbContext>();
    }

    /// <summary>
    /// This method can be used for create a new dbContext (different database) with different name in run-time.
    /// It is useful when you want to work on an empty database.
    /// </summary>
    /// <param name="dbContextName"></param>
    /// <returns>Creates new dbContext (new database) with different name</returns>
    protected WebAppDbContext GetNewTestDbContext(string dbContextName)
    {
        var provider = GetNewHostServiceProvider().CreateScope().ServiceProvider;

        var dbContextOptionBuilder = new DbContextOptionsBuilder<WebAppDbContext>();
        dbContextOptionBuilder.UseInMemoryDatabase(dbContextName)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging();

        return new WebAppDbContext(dbContextOptionBuilder.Options);
    }

    protected IServiceProvider GetNewHostServiceProvider()
    {
        return GetTestServer().Services;
    }

    protected TestServer GetTestServer()
    {
        return new TestServer(
            new WebHostBuilder()
                .UseStartup<TestStartup>()
                .UseEnvironment("Test")
        );
    }
}
