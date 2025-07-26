using TinyFileSentry.Core.Models;

namespace TinyFileSentry.Core.Interfaces;

public interface IPollerService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
    bool IsRunning { get; }
    event EventHandler<string>? FileChanged;
    event EventHandler<(string FilePath, RuleStatus Status)>? RuleStatusChanged;
}