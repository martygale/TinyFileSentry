using TinyFileSentry.Core.Models;

namespace TinyFileSentry.Core.Interfaces;

public interface ILogService
{
    void Log(LogLevel level, string message, string? source = null);
    void Info(string message, string? source = null);
    void Warning(string message, string? source = null);
    void Error(string message, string? source = null);
    IEnumerable<LogEntry> GetLogs();
    event EventHandler<LogEntry>? LogAdded;
}