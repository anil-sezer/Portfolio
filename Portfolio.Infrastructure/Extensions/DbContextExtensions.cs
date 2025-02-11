using DotNetEnv;
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
        var connectionString = builder.GetConnectionStringForPostgres();

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

        // todo: I should create a solution for this, I don't want commented out code. 
        // comment this if you wanna create migrations. It confuses EF.
        // if (builder.Environment.IsDevelopment())
        //     AutoMigrate(builder);
    }

    private static void AutoMigrate(WebApplicationBuilder builder)
    {
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        dbContext.Database.Migrate();
    }
    
    private static string GetConnectionStringForPostgres(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
            Env.Load("../.env");

        var host     = Environment.GetEnvironmentVariable("SQL_DB_HOST");
        var port     = Environment.GetEnvironmentVariable("SQL_DB_PORT");
        var userName = Environment.GetEnvironmentVariable("SQL_DB_USER");
        var userPass = Environment.GetEnvironmentVariable("SQL_DB_PASSWORD");
        var dbName   = Environment.GetEnvironmentVariable("SQL_DB_NAME");

        CheckDbParams(host, port, userName, userPass, dbName);

        // todo: Stop using default public schema
        return $"Host={host};Port={port};Username={userName};Password={userPass};Database={dbName};";
    }
    
    private static void CheckDbParams(string? host, string? port, string? userName, string? userPass, string? dbName)
    {
        if (!string.IsNullOrEmpty(host) &&
            !string.IsNullOrEmpty(port) &&
            !string.IsNullOrEmpty(userName) &&
            !string.IsNullOrEmpty(userPass) &&
            !string.IsNullOrEmpty(dbName)) 
            return;
        
        Log.Fatal("One or more db ConnectionString value(s) is not set. Params: Host: {Host}, Port: {Port}, UserName: {UserName}, UserPass: {UserPass}, DbName: {DbName}", 
            host, port, userName, userPass, dbName);
        throw new InvalidOperationException();
    }
    
    private static void ApplyMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var environment = services.GetRequiredService<IHostEnvironment>();

        if (environment.IsDevelopment())
        {
            try
            {
                var dbContext = services.GetRequiredService<PortfolioDbContext>();
                dbContext.Database.Migrate();
                Log.Information("Database migrated successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while applying migrations");
            }
        }
    }
}