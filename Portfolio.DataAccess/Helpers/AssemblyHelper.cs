using System.Reflection;
using Serilog;

namespace Portfolio.DataAccess.Helpers;

public static class AssemblyHelper
{
    public static string GetStartupProjectsName()
    {
        var entryAssembly = Assembly.GetEntryAssembly();

        if (entryAssembly == null)
        {
            Log.Warning("Could not get the name of the startup project via Assembly.GetEntryAssembly()");
            var stackTrace = new System.Diagnostics.StackTrace(true);
            var callingAssembly = stackTrace.GetFrames()?.LastOrDefault()?.GetMethod()?.Module.Assembly;

            if (callingAssembly != null)
            {
                entryAssembly = callingAssembly;
            }
            else
            {
                // todo-anil-exception: Custom exceptions needed! 
                const string err = "Cannot get the project name via stack trace.";
                Log.Error(err);
                throw new Exception(err);
            }
        }

        string assemblyLocation = entryAssembly.Location;
        string startupProjectName = Path.GetFileNameWithoutExtension(assemblyLocation);

        return startupProjectName;
    }
}