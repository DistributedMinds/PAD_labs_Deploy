using System.Text;

namespace Broker_gRPC.Logging;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly object _locker = new();

    public FileLoggerProvider(string filePath)
    {
        _filePath = filePath;
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this);

    public void Dispose()
    {
    }

    private void Write(string line)
    {
        // Lock: mai multe thread-uri (gRPC + worker) pot loga simultan
        lock (_locker)
        {
            File.AppendAllText(_filePath, line + Environment.NewLine);
        }
    }

    private class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;

        public FileLogger(FileLoggerProvider provider)
        {
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel >= LogLevel.Information && logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string level = logLevel switch
            {
                LogLevel.Warning => "WARNING",
                LogLevel.Error or LogLevel.Critical => "ERROR",
                _ => "INFO"
            };

            var line = new StringBuilder();
            line.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level} {formatter(state, exception)}");

            if (exception != null)
            {
                line.Append($" | {exception.GetType().Name}: {exception.Message}");
            }

            try
            {
                _provider.Write(line.ToString());
            }
            catch
            {
                // Un eșec la scrierea logului nu trebuie să oprească brokerul
            }
        }
    }
}