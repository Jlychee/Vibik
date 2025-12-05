using System.Text;

namespace Infrastructure.Utils;

public static class AppLogger
{
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private static string? logDirectory;

    public static void Initialize(string baseDir)
    {
        logDirectory = Path.Combine(baseDir, "logs");
        Directory.CreateDirectory(logDirectory);
    }

    private static string GetLogFilePath()
    {
        if (logDirectory is null)
        {
            logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
        }

        var fileName = $"vibik-{DateTime.UtcNow:yyyyMMdd}.log";
        return Path.Combine(logDirectory, fileName);
    }

    private static async Task WriteAsync(string level, string message, Exception? ex = null)
    {
        await Lock.WaitAsync();
        try
        {
            var sb = new StringBuilder();
            sb.Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
            sb.Append(" [").Append(level).Append("] ");
            sb.Append(message);

            if (ex != null)
            {
                sb.AppendLine();
                sb.AppendLine(ex.ToString());
            }

            sb.AppendLine();

            var path = GetLogFilePath();
            await File.AppendAllTextAsync(path, sb.ToString());
        }
        finally
        {
            Lock.Release();
        }
    }

    public static Task Info(string message) => WriteAsync("INFO",  message);
    public static Task Warn(string message) => WriteAsync("WARN",  message);
    public static Task Error(string message) => WriteAsync("ERROR", message);
    public static Task Error(string message, Exception ex) => WriteAsync("ERROR", message, ex);
}