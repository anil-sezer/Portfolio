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

        var appName = AssemblyHelper.GetStartupProjectsName();
        builder.Services.AddDbContext<PortfolioDbContext>(options =>
        {
            options.UseNpgsql(connectionString + $";Application Name= {appName}",
                npgsqlOptionsAction: sqlOptions =>
                {
                    // sqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, dbSchemaName);
                    // sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                }).UseSnakeCaseNamingConvention();

            if (!builder.Environment.IsDevelopment()) return;
            
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        });
    }
    
    private static string GetConnectionStringForPostgres()
    {
        // todo: Stop using default public schema
        return $"Host={EnvVars.SQL_DB_HOST};Port={EnvVars.SQL_DB_PORT};Username={EnvVars.SQL_DB_USER};Password={EnvVars.SQL_DB_PASSWORD};Database={EnvVars.SQL_DB_NAME};";
    }
    
    public static void AutoMigrateInDevEnv(this WebApplication app)
    {
        if (!EnvVars.IsDevelopment())
            return;

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    
        try
        {
            context.Database.Migrate();
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating the database");
            throw;
        }
    }
}
