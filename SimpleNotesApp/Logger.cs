using System;
using System.IO;

namespace SimpleNotesApp;

public static class Logger
{
    private static string logFilePath;

    static Logger()
    {
        string exeDirectory = AppContext.BaseDirectory;
        string logFolder = System.IO.Path.Combine(exeDirectory, "Logs");
        
        if (!Directory.Exists(logFolder))
        {
            Directory.CreateDirectory(logFolder);
        }
        
        logFilePath = System.IO.Path.Combine(logFolder, $"error_{DateTime.Now:yyyyMMdd}.log");
    }

    public static void Error(string message, Exception? ex = null)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ERROR: {message}");
                if (ex != null)
                {
                    writer.WriteLine($"Exception: {ex.Message}");
                    writer.WriteLine($"Stack Trace: {ex.StackTrace}");
                }
                writer.WriteLine();
            }
        }
        catch (Exception)
        {
            // 防止日志记录本身失败导致应用崩溃
        }
    }

    public static void Info(string message)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] INFO: {message}");
                writer.WriteLine();
            }
        }
        catch (Exception)
        {
            // 防止日志记录本身失败导致应用崩溃
        }
    }
}