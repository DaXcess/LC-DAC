using BepInEx.Logging;

namespace DAC;

public static class Logger
{
    private static ManualLogSource source;
    
    public static void SetSource(ManualLogSource source)
    {
        Logger.source = source;
    }

    public static void Log(object data)
    {
        source.Log(LogLevel.Info, data);
    }

    public static void LogInfo(object data)
    {
        source.Log(LogLevel.Info, data);
    }

    public static void LogError(object data)
    {
        source.Log(LogLevel.Error, data);
    }

    public static void LogWarning(object data)
    {
        source.Log(LogLevel.Warning, data);
    }

    public static void LogDebug(object data)
    {
        source.Log(LogLevel.Debug, data);
    }
}