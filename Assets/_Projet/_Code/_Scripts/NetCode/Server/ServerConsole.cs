using System;
using System.IO;
using UnityEngine;

public class ServerConsole
{
    public enum LogType
    {
        Debug,
        Info,
        Warning,
        Error
    }

    private static string _logFilePath = string.Empty;

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

#if UNITY_SERVER && !UNITY_EDITOR
        Console.WriteLine(logString);
        SaveLog(logString);
#endif
    }

    private static void SaveLog(string logString)
    {
        if (_logFilePath == string.Empty)
        {
            string filepath = string.Empty;
            filepath = Directory.GetParent(Application.dataPath).FullName;
            filepath = Path.Combine(filepath, "Logs");

            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }

            string date = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            filepath = Path.Combine(filepath, date + ".txt");

            _logFilePath = filepath;
        }

        File.AppendAllText(_logFilePath, logString + Environment.NewLine);
    }
}
