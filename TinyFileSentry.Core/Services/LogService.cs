using System.Collections.Concurrent;
using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Models;

namespace TinyFileSentry.Core.Services;

public class LogService : ILogService
{
    private readonly ConcurrentQueue<LogEntry> _logs = new();
    private readonly IClock _clock;
    private const int MaxLogEntries = 10000;

    public event EventHandler<LogEntry>? LogAdded;

    public LogService(IClock clock)
    {
        _clock = clock;
    }

    public void Log(LogLevel level, string message, string? source = null)
    {
        var logEntry = new LogEntry
        {
            Timestamp = _clock.UtcNow,
            Level = level,
            Message = message,
            Source = source
        };

        _logs.Enqueue(logEntry);

        while (_logs.Count > MaxLogEntries)
        {
            _logs.TryDequeue(out _);
        }

        LogAdded?.Invoke(this, logEntry);
    }

    public void Info(string message, string? source = null) => Log(LogLevel.Info, message, source);

    public void Warning(string message, string? source = null) => Log(LogLevel.Warning, message, source);

    public void Error(string message, string? source = null) => Log(LogLevel.Error, message, source);

    public IEnumerable<LogEntry> GetLogs() => _logs.ToArray();
}