using System.Text;

namespace TIAExporter.Logging;

internal sealed class ExportLogger : IDisposable
{
    private readonly object sync = new();
    private readonly Action<string>? sink;
    private StreamWriter? writer;

    private ExportLogger(Action<string>? sink)
    {
        this.sink = sink;
    }

    public event Action<string>? MessageWritten;

    public static ExportLogger CreateConsoleLogger() => new(message => Console.WriteLine(message));

    public static ExportLogger CreateGuiLogger(Action<string> sink) => new(sink);

    public void AttachFile(string logPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        writer = new StreamWriter(logPath, append: false, new UTF8Encoding(false)) { AutoFlush = true };
    }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message, Exception? ex = null)
    {
        Write("ERROR", ex == null ? message : $"{message}: {ex.GetType().Name}: {ex.Message}");
        if (ex?.StackTrace != null)
        {
            Write("TRACE", ex.StackTrace);
        }
    }

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}";
        lock (sync)
        {
            writer?.WriteLine(line);
        }

        sink?.Invoke(line);
        MessageWritten?.Invoke(line);
    }

    public void Dispose()
    {
        lock (sync)
        {
            writer?.Dispose();
            writer = null;
        }
    }
}
