using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.DataAccess;

namespace Portfolio.Test.Shared;

public class TestStartup
{
    public TestStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }
        
    public IConfiguration Configuration { get; }
        
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<WebAppDbContext>(options =>
        {
            options.UseInMemoryDatabase("DbContextTest")
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .ConfigureWarnings(x=>x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        });
    }

    public void Configure()
    {
    }
}