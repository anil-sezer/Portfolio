using Portfolio.Infrastructure.Constants;
using Portfolio.Infrastructure.Exceptions;
using Serilog;

namespace Portfolio.Infrastructure.Helpers;

public static class EnvVarHelpers
{
    public static bool VerifyEnvironmentValueIsSet(string variableName, bool triggerExceptionIfMissing = true)
    {
        var val = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrEmpty(val) && triggerExceptionIfMissing)
        {
            Log.Fatal("This environment value is not set: {MissingValues}", variableName);
            throw new MissingEnvironmentValueException(variableName); 
        }

        return false;
    }
    
    // todo: This is not working properly. Fix it
    public static void VerifyEnvironmentValuesAreSet(string[] variableNames)
    {
        var missingValues = variableNames
            .Where(x => VerifyEnvironmentValueIsSet(x, false))
            .ToList();

        if (missingValues.Count == 0) 
            return;
        
        
        Log.Fatal("One or more environment values are not set. Missing values: {MissingValues}", missingValues);
        throw new MissingEnvironmentValueException("Missing: " + string.Join(", ", missingValues));
    }
    
    public static string GetValue(string variableName)
    {
        return GetValue<string>(variableName);
    }

    public static T GetValue<T>(string variableName)
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
    
    public static bool IsDevelopment()
    {
        var env = Environment.GetEnvironmentVariable(EnvVarNames.DevOrProd);
        return string.IsNullOrEmpty(env) || env.Equals("Development", StringComparison.OrdinalIgnoreCase);
    }
}