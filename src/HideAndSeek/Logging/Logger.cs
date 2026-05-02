using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using BepInEx.Logging;

namespace OneLetterShor.HideAndSeek.Logging;

public static class Logger
{
    public static int NextMarkIndex => Interlocked.Increment(ref MarkIndex);
    public static int MarkIndex = 0;
    
    private static Dictionary<string, string> _relativePathByPath = new(StringComparer.Ordinal);
    
    /// <summary>
    /// Convenience logging method used to both label messages with a number
    /// and to separate from other messages with multiple equal signs.
    /// </summary>
    /// <inheritdoc cref="Log"/>
    public static void Mark(
        object? data = null,
        LogLevel logLevels = LogLevel.Message,
        [CallerMemberName] string loggingMemberName = "",
        [CallerLineNumber] int loggingLineNumber = 0,
        [CallerFilePath] string loggingFilePath = "")
    {
        string suffix = data is null
            ? ""
            : $": {data}";
        
        Log(logLevels, $"============================== MARK {NextMarkIndex}{suffix} ==============================", loggingMemberName, loggingLineNumber, loggingFilePath);
    }
    
    /// <summary>
    /// Logs a <see cref="LogLevel.Fatal"/> message if that log level
    /// is enabled in <see cref="Cfg.Options.EnabledLogLevels"/>.
    /// </summary>
    /// <inheritdoc cref="Log"/>
    public static void Fatal(
        object? data,
        [CallerMemberName] string loggingMemberName = "",
        [CallerLineNumber] int loggingLineNumber = 0,
        [CallerFilePath] string loggingFilePath = "")
    {
        Log(LogLevel.Fatal, data, loggingMemberName, loggingLineNumber, loggingFilePath);
    }
    
    /// <summary>
    /// Logs a <see cref="LogLevel.Error"/> message if that log level
    /// is enabled in <see cref="Cfg.Options.EnabledLogLevels"/>.
    /// </summary>
    /// <inheritdoc cref="Log"/>
    public static void Error(
        object? data,
        [CallerMemberName] string loggingMemberName = "",
        [CallerLineNumber] int loggingLineNumber = 0,
        [CallerFilePath] string loggingFilePath = "")
    {
        Log(LogLevel.Error, data, loggingMemberName, loggingLineNumber, loggingFilePath);
    }
    
    /// <summary>
    /// Logs a <see cref="LogLevel.Warning"/> message if that log level
    /// is enabled in <see cref="Cfg.Options.EnabledLogLevels"/>.
    /// </summary>
    /// <inheritdoc cref="Log"/>
    public static void Warning(
        object? data,
        [CallerMemberName] string loggingMemberName = "",
        [CallerLineNumber] int loggingLineNumber = 0,
        [CallerFilePath] string loggingFilePath = "")
    {
        Log(LogLevel.Warning, data, loggingMemberName, loggingLineNumber, loggingFilePath);
    }
    
    /// <summary>
    /// Logs a <see cref="LogLevel.Message"/> message if that log level
    /// is enabled in <see cref="Cfg.Options.EnabledLogLevels"/>.
    /// </summary>
    /// <inheritdoc cref="Log"/>
    public static void Message(
        object? data,
        [CallerMemberName] string loggingMemberName = "",
        [CallerLineNumber] int loggingLineNumber = 0,
        [CallerFilePath] string loggingFilePath = "")
    {
        Log(LogLevel.Message, data, loggingMemberName, loggingLineNumber, loggingFilePath);
    }
    
    /// <summary>
    /// Logs a <see cref="LogLevel.Info"/> message if that log level
    /// is enabled in <see cref="Cfg.Options.EnabledLogLevels"/>.
    /// </summary>
    /// <inheritdoc cref="Log"/>
    public static void Info(
        object? data,
        [CallerMemberName] string loggingMemberName = "",
        [CallerLineNumber] int loggingLineNumber = 0,
        [CallerFilePath] string loggingFilePath = "")
    {
        Log(LogLevel.Info, data, loggingMemberName, loggingLineNumber, loggingFilePath);
    }
    
    /// <summary>
    /// Logs a <see cref="LogLevel.Debug"/> message if that log level
    /// is enabled in <see cref="Cfg.Options.EnabledLogLevels"/>.
    /// </summary>
    /// <inheritdoc cref="Log"/>
    public static void Debug(
        object? data,
        [CallerMemberName] string loggingMemberName = "",
        [CallerLineNumber] int loggingLineNumber = 0,
        [CallerFilePath] string loggingFilePath = "")
    {
        Log(LogLevel.Debug, data, loggingMemberName, loggingLineNumber, loggingFilePath);
    }
    
    /// <summary>
    /// Logs a message if at least one flag in <paramref name="logLevels"/> is enabled.
    /// </summary>
    /// <param name="logLevels">The <see cref="LogLevel"/> that the data is logged at.</param>
    /// <param name="data">The data that is logged.</param>
    /// <param name="loggingMemberName">Caller info</param>
    /// <param name="loggingLineNumber">Caller info</param>
    /// <param name="loggingFilePath">Caller info</param>
    /// <remarks>
    /// Logging is allowed when either is <see langword="true"/>:
    /// <br/>- At least one flag in <paramref name="logLevels"/> is enabled in <see cref="Cfg.Options.EnabledLogLevels"/>.
    /// <br/>- <paramref name="logLevels"/> is <see cref="LogLevel.None"/> and <see cref="Cfg.Options.EnabledLogLevels"/> is not <see cref="LogLevel.None"/>.<br/>
    /// NOTE: If messages are logged before configurables are initialized then the
    /// default <see cref="Cfg.Options.CfgEnabledLogLevels"/> value will be used.
    /// </remarks>
    public static void Log(
        LogLevel logLevels,
        object? data,
        [CallerMemberName] string loggingMemberName = "",
        [CallerLineNumber] int loggingLineNumber = 0,
        [CallerFilePath] string loggingFilePath = "")
    {
        int logLevelsValue = (int)logLevels;
        int enabledLogLevelsValue = (int)Plugin.Options.EnabledLogLevels;
        if ((logLevelsValue & enabledLogLevelsValue) == 0 && (logLevelsValue != 0 && enabledLogLevelsValue != 0)) return;
        
        StringBuilder stringBuilder = GenerateLogCallerInfoOptimized(loggingMemberName, loggingLineNumber, loggingFilePath);
        
        stringBuilder
            .Append(": ")
            .Append(data);
        
        Plugin.__Logger.Log(logLevels, stringBuilder.ToString()); // $"{TrimPath(loggingFilePath)}:{loggingLineNumber}, {loggingMemberName}(): {data}"
    }
    
    /// <summary>
    /// Behaves the same as <see cref="Log"/> except messages are always logged
    /// regardless of <see cref="Cfg.Options.EnabledLogLevels"/>'s value. 
    /// </summary>
    /// <inheritdoc cref="Log"/>
    public static void ForceLog(
        LogLevel logLevels,
        object? data,
        [CallerMemberName] string loggingMemberName = "",
        [CallerLineNumber] int loggingLineNumber = 0,
        [CallerFilePath] string loggingFilePath = "")
    {
        StringBuilder stringBuilder = GenerateLogCallerInfoOptimized(loggingMemberName, loggingLineNumber, loggingFilePath);
        
        stringBuilder
            .Append(" [forced]: ")
            .Append(data);
        
        Plugin.__Logger.Log(logLevels, stringBuilder.ToString()); // $"{TrimPath(loggingFilePath)}:{loggingLineNumber}, {loggingMemberName}() [forced]: {data}"
    }
    
    /// <summary>
    /// Generates logger information used for prefixing log messages.
    /// </summary>
    /// <param name="loggingMemberName">Caller info</param>
    /// <param name="loggingLineNumber">Caller info</param>
    /// <param name="loggingFilePath">Caller info</param>
    /// <returns>Log caller info as a <see cref="StringBuilder"/>.</returns>
    public static StringBuilder GenerateLogCallerInfoOptimized(
        [CallerMemberName] string loggingMemberName = "",
        [CallerLineNumber] int loggingLineNumber = 0,
        [CallerFilePath] string loggingFilePath = "")
    {
        StringBuilder stringBuilder = new(TrimPath(loggingFilePath), 100);
        
        stringBuilder
            .Append(':')
            .Append(loggingLineNumber)
            .Append(", ")
            .Append(loggingMemberName)
            .Append("()");
        
        return stringBuilder;
        
        static string TrimPath(string path)
        {
            if (_relativePathByPath.TryGetValue(path, out string relativePath))
                return relativePath; // Happy path.
            
            string[] elements = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            int srcDirIndex = Array.IndexOf(elements, "src");
            int projectDirIndex = srcDirIndex + 1;
            
            if (srcDirIndex == -1 || projectDirIndex >= elements.Length - 1)
            {
                StackTrace stackTrace = new(0, true);
                Plugin.__Logger.LogError($"Path '{path}' does not source code directory 'src'. Using full path for logging.\n{stackTrace}");
                return _relativePathByPath[path] = path;
            }
            
            relativePath = string.Join(Path.AltDirectorySeparatorChar.ToString(), elements.Skip(projectDirIndex + 1).ToArray());
            
            _relativePathByPath[path] = relativePath;
            return relativePath; // Only runs once per path.
        }
    }
    
    /// <inheritdoc cref="GenerateLogCallerInfoOptimized"/>
    /// <returns>Log caller info as a <see langword="string"/>.</returns>
    public static string GenerateLogCallerInfo(
        [CallerMemberName] string loggingMemberName = "",
        [CallerLineNumber] int loggingLineNumber = 0,
        [CallerFilePath] string loggingFilePath = "")
    {
        return GenerateLogCallerInfoOptimized(loggingMemberName, loggingLineNumber, loggingFilePath).ToString();
    }
}