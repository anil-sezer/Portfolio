using Portfolio.Infrastructure.Exceptions;
using Serilog;

namespace Portfolio.Infrastructure.Extensions;

public static class EnvironmentExtensions
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
}