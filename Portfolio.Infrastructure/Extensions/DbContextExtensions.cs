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
    
    public static async Task AutoMigrateInDevEnvAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        if (!EnvVars.IsDevelopment())
            return;

        try
        {
            Log.Information("Applying database migrations in development environment...");
            await using var scope = app.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetService<PortfolioDbContext>();

            await context.Database.MigrateAsync(cancellationToken);
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while applying database migrations in development environment");
            throw;
        }
    }
}
