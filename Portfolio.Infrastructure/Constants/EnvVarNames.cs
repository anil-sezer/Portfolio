// ReSharper disable InconsistentNaming
namespace Portfolio.Infrastructure.Constants;

public static class EnvVarNames
{
    public const string Grpc_BaseUrl = "GRPC_BASE_URL";
    
    public const string SqlDb_Host = "SQL_DB_HOST";
    public const string SqlDb_Port = "SQL_DB_PORT";
    public const string SqlDb_User = "SQL_DB_USER";
    public const string SqlDb_Password = "SQL_DB_PASSWORD";
    public const string SqlDb_Name = "SQL_DB_NAME";
    
    public const string OpenTelemetry_CollectorEndpoint = "OTEL_COLLECTOR_ENDPOINT";
    
    public const string DevOrProd = "ASPNETCORE_ENVIRONMENT";
    
    public const string Email_Slack_WebhookUrl = "PORTFOLIO_EMAIL_SLACK_WEBHOOK_URL";
}
