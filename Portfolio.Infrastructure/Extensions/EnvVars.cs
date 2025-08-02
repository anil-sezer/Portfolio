using Portfolio.Infrastructure.Exceptions;
using Serilog;

// ReSharper disable InconsistentNaming

namespace Portfolio.Infrastructure.Extensions;

/// <summary>
/// Provides direct access to environment variables with built-in type conversion
/// </summary>
public static class EnvVars
{
    public static string NOTIFICATION_TELEGRAM_API_KEY => GetValue("NOTIFICATION_TELEGRAM_API_KEY");
    public static string NOTIFICATION_TELEGRAM_CHAT_ID => GetValue("NOTIFICATION_TELEGRAM_CHAT_ID");
    
    public static string GRPC_BASE_URL => GetValue("GRPC_BASE_URL");

    // Database
    public static string SQL_DB_HOST => GetValue("SQL_DB_HOST");
    public static string SQL_DB_PORT => GetValue("SQL_DB_PORT");
    public static string SQL_DB_USER => GetValue("SQL_DB_USER");
    public static string SQL_DB_PASSWORD => GetValue("SQL_DB_PASSWORD");
    public static string SQL_DB_NAME => GetValue("SQL_DB_NAME");
    
    // Telemetry
    public static string OTEL_COLLECTOR_ENDPOINT => GetValue("OTEL_COLLECTOR_ENDPOINT");
    
    // Environment
    public static string ASPNETCORE_ENVIRONMENT => GetValue("ASPNETCORE_ENVIRONMENT");
    public static bool IsDevelopment() => string.IsNullOrEmpty(ASPNETCORE_ENVIRONMENT) || 
                                          ASPNETCORE_ENVIRONMENT.Equals("Development", StringComparison.OrdinalIgnoreCase);
    
    // Helper methods (internal implementation)
    private static string GetValue(string variableName)
    {
        return GetValue<string>(variableName);
    }

    private static T GetValue<T>(string variableName)
    {
        var val = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrEmpty(val))
        {
            Log.Error("This environment value is not set: {MissingValues}", variableName);
            throw new MissingEnvironmentValueException(variableName); 
        }

        try
        {
            return (T)Convert.ChangeType(val, typeof(T));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to convert environment variable {VariableName} with value '{Value}' to type {Type}", 
                variableName, val, typeof(T).Name);
            throw new InvalidCastException($"Cannot convert environment variable '{variableName}' with value '{val}' to type {typeof(T).Name}", ex);
        }
    }
}
