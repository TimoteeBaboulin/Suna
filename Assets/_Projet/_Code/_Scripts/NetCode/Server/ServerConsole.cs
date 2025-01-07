using System;
using UnityEngine;

public class ServerConsol
{
    public enum LogType
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public static void Log(LogType logType, string message)
    {
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        if (logType == LogType.Debug)
        {
            return;
        }
#endif

        string logString = string.Empty;

        logString += DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ");

        logString += logType switch
        {
            LogType.Debug => "[DEBUG] ",
            LogType.Info => "[INFO] ",
            LogType.Warning => "[WARNING] ",
            LogType.Error => "[ERROR] ",
            _ => "[INFO] ",
        };

        logString += message;

        Console.WriteLine(logString);
    }
}
