using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Portfolio.Infrastructure.Helpers;
using Serilog;

namespace Portfolio.Infrastructure.Extensions;

public static class DbContextExtensions
{
    public static void InitDbWithPostgres(this WebApplicationBuilder builder)
    {
        var connectionString = GetConnectionStringForPostgres();

        builder.Services.AddDbContext<PortfolioDbContext>(options =>
        {
            options.UseNpgsql(connectionString + $";Application Name= {AssemblyHelper.GetServiceName()}");
            options.UseSnakeCaseNamingConvention();

            if (!builder.Environment.IsDevelopment()) return;
            
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        });
    }
    
    private static string GetConnectionStringForPostgres()
    {
        return $"Host={EnvVars.SQL_DB_HOST};Port={EnvVars.SQL_DB_PORT};Username={EnvVars.SQL_DB_USER};Password={EnvVars.SQL_DB_PASSWORD};Database={EnvVars.SQL_DB_NAME};";
    }
    
    public static void AutoMigrateInDevEnv(this WebApplication app)
    {
        if (!EnvVars.IsDevelopment())
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
                await context.Database.MigrateAsync();
                Log.Information("Database migrations applied successfully in background");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while migrating the database in background");
            }
        });
    }
}
