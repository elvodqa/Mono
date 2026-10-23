using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Mono;

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warn,
    Error,
    Fatal
}

public static class Logger
{
    private static readonly object _lock = new();

    private static StreamWriter? _writer;
    private static bool _disposed;

    public static LogLevel MinLevel { get; set; } = LogLevel.Trace;

    public static bool LogToFile { get; private set; }
    public static string? FilePath { get; private set; }
    public static Mono.Debug.Console? Console { get; set; }

    public static void EnableFileLogging(string? filePath = null)
    {
        lock (_lock)
        {
            if (_writer != null)
                return;

            LogToFile = true;
            FilePath = filePath ?? $"logs/log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";

            string? directory = Path.GetDirectoryName(FilePath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            _writer = new StreamWriter(FilePath, append: true)
            {
                AutoFlush = true
            };
        }
    }

    public static void SetConsole(Mono.Debug.Console? console = null)
    {
        lock (_lock)
        {
            if (console == null)
                return;
            Console = console;
        }
    }

    public static void Log(
        LogLevel level,
        string message,
        [CallerMemberName] string member = "",
        [CallerFilePath] string file = "")
    {
        if (_disposed)
            return;

        if (level < MinLevel)
            return;

        string className = Path.GetFileNameWithoutExtension(file);
        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        string line = $"[{time}] [{level}] [{className}.{member}] {message}";

        lock (_lock)
        {
            System.Console.WriteLine(line);
            Console?.AddConsoleItem(line);
            _writer?.WriteLine(line);
        }
    }

    public static void Trace(
        string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "")
        => Log(LogLevel.Trace, msg, m, f);

    public static void Debug(
        string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "")
        => Log(LogLevel.Debug, msg, m, f);

    public static void Info(
        string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "")
        => Log(LogLevel.Info, msg, m, f);

    public static void Warn(
        string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "")
        => Log(LogLevel.Warn, msg, m, f);

    public static void Error(
        string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "")
        => Log(LogLevel.Error, msg, m, f);

    public static void Fatal(
        string msg,
        [CallerMemberName] string m = "",
        [CallerFilePath] string f = "")
        => Log(LogLevel.Fatal, msg, m, f);

    public static void Shutdown()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
            _disposed = true;
        }
    }
}