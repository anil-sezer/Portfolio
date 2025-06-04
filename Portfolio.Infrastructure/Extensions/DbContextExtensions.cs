using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Portfolio.Infrastructure.Constants;
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
        var host = EnvVarHelpers.GetValue(EnvVarNames.SqlDb_Host);
        var port     = EnvVarHelpers.GetValue(EnvVarNames.SqlDb_Port);
        var userName = EnvVarHelpers.GetValue(EnvVarNames.SqlDb_User);
        var userPass = EnvVarHelpers.GetValue(EnvVarNames.SqlDb_Password);
        var dbName = EnvVarHelpers.GetValue(EnvVarNames.SqlDb_Name);

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
    
    public static void AutoMigrateInDevEnv(this WebApplication app)
    {
        if (!EnvVarHelpers.IsDevelopment())
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
