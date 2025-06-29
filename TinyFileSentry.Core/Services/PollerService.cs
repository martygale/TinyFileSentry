using TinyFileSentry.Core.Extensions;
using TinyFileSentry.Core.Interfaces;

namespace TinyFileSentry.Core.Services;

public class PollerService : IPollerService
{
    private readonly IRulesService _rulesService;
    private readonly IFileSystem _fileSystem;
    private readonly IHashService _hashService;
    private readonly ICopyService _copyService;
    private readonly IPostCopyService _postCopyService;
    private readonly ILogService _logService;
    private readonly IClock _clock;
    private readonly IConfigurationService _configurationService;
    private readonly IPathSanitizer _pathSanitizer;
    
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _pollingTask;

    public bool IsRunning { get; private set; }
    public event EventHandler<string>? FileChanged;
    public event EventHandler<(string FilePath, Models.RuleStatus Status)>? RuleStatusChanged;

    public PollerService(
        IRulesService rulesService,
        IFileSystem fileSystem,
        IHashService hashService,
        ICopyService copyService,
        IPostCopyService postCopyService,
        ILogService logService,
        IClock clock,
        IConfigurationService configurationService,
        IPathSanitizer pathSanitizer)
    {
        _rulesService = rulesService;
        _fileSystem = fileSystem;
        _hashService = hashService;
        _copyService = copyService;
        _postCopyService = postCopyService;
        _logService = logService;
        _clock = clock;
        _configurationService = configurationService;
        _pathSanitizer = pathSanitizer;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logService.Warning("Poller service is already running", nameof(PollerService));
            return Task.CompletedTask;
        }

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pollingTask = PollFilesAsync(_cancellationTokenSource.Token);
        IsRunning = true;
        
        _logService.Info("Poller service started", nameof(PollerService));
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!IsRunning)
            return;

        _cancellationTokenSource?.Cancel();
        
        if (_pollingTask != null)
        {
            await _pollingTask;
        }

        IsRunning = false;
        _logService.Info("Poller service stopped", nameof(PollerService));
    }

    // Infrastructure method - does not require test coverage
    private async Task PollFilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ProcessAllRulesAsync(cancellationToken);
                await WaitForNextPollAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logService.Info("Polling cancelled", nameof(PollerService));
        }
        catch (Exception ex)
        {
            _logService.Error($"Polling error: {ex.Message}", nameof(PollerService));
        }
    }

    // Testable method - processes all rules
    internal async Task ProcessAllRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = _rulesService.GetWatchRules();
        
        foreach (var rule in rules)
        {
            await ProcessRuleAsync(rule, cancellationToken);
        }
    }

    // Infrastructure method - wait between polling cycles
    private async Task WaitForNextPollAsync(CancellationToken cancellationToken)
    {
        var configuration = _configurationService.LoadConfiguration();
        var intervalMs = configuration.PollingSpeed.ToSeconds() * 1000;
        await Task.Delay(intervalMs, cancellationToken);
    }

    // Testable method - process single rule
    internal async Task ProcessRuleAsync(Models.WatchRule rule, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ShouldProcessRule(rule))
            {
                return;
            }

            if (!HasFileChanged(rule))
            {
                // Files are identical - synchronized
                rule.Status = Models.RuleStatus.Synchronized;
                return;
            }

            _logService.Info($"File changed detected: {rule.SourceFile}", nameof(PollerService));
            FileChanged?.Invoke(this, rule.SourceFile);

            var copied = await _copyService.CopyFileAsync(rule.SourceFile, rule.DestinationRoot, cancellationToken);
            
            if (copied)
            {
                await ExecutePostCopyActionsAsync(rule, cancellationToken);
                // After successful copying, files are synchronized
                rule.Status = Models.RuleStatus.Synchronized;
                RuleStatusChanged?.Invoke(this, (rule.SourceFile, rule.Status));
            }
            else
            {
                // Copy failed - error
                rule.Status = Models.RuleStatus.Error;
                RuleStatusChanged?.Invoke(this, (rule.SourceFile, rule.Status));
            }
        }
        catch (Exception ex)
        {
            _logService.Error($"Error processing rule for {rule.SourceFile}: {ex.Message}", nameof(PollerService));
            rule.Status = Models.RuleStatus.Error;
            RuleStatusChanged?.Invoke(this, (rule.SourceFile, rule.Status));
        }
    }

    // Testable method - check if rule should be processed
    internal bool ShouldProcessRule(Models.WatchRule rule)
    {
        if (!_fileSystem.FileExists(rule.SourceFile))
        {
            rule.Status = Models.RuleStatus.SourceDeleted;
            RuleStatusChanged?.Invoke(this, (rule.SourceFile, rule.Status));
            return false;
        }
        
        rule.Status = Models.RuleStatus.Synchronized;
        return true;
    }

    // Testable method - check if file has changed
    internal bool HasFileChanged(Models.WatchRule rule)
    {
        var sourceHash = _hashService.ComputeHash(rule.SourceFile);
        var destinationPath = BuildDestinationFilePath(rule);
        
        // If destination file doesn't exist, copy is needed
        if (!_fileSystem.FileExists(destinationPath))
        {
            return true;
        }
        
        var destinationHash = _hashService.ComputeHash(destinationPath);
        return sourceHash != destinationHash;
    }

    // Testable method - execute post-copy actions
    internal async Task ExecutePostCopyActionsAsync(Models.WatchRule rule, CancellationToken cancellationToken = default)
    {
        var destinationPath = BuildDestinationPath(rule);
        
        if (string.IsNullOrEmpty(destinationPath))
        {
            return;
        }

        var fullDestinationPath = BuildDestinationFilePath(rule);
        
        var postActionSuccess = await _postCopyService.ExecutePostActionAsync(
            rule.PostAction, 
            fullDestinationPath, 
            destinationPath, 
            rule.SourceFile, 
            cancellationToken);

        if (postActionSuccess)
        {
            UpdateRuleAfterSuccessfulCopy(rule);
        }
    }

    // Testable method - build destination path
    internal string? BuildDestinationPath(Models.WatchRule rule)
    {
        var sourceDirectory = Path.GetDirectoryName(rule.SourceFile) ?? string.Empty;
        var sanitizedPath = _pathSanitizer.SanitizePath(sourceDirectory);
        
        // If sanitized path is empty or is a root directory (one letter + underscore)
        if (string.IsNullOrEmpty(sanitizedPath) || sanitizedPath.Length <= 2)
        {
            return rule.DestinationRoot;
        }
        
        // Return full path to destination directory, including sanitized path
        return Path.Combine(rule.DestinationRoot, sanitizedPath);
    }

    // Testable method - build full destination file path  
    internal string BuildDestinationFilePath(Models.WatchRule rule)
    {
        var destinationDirectory = BuildDestinationPath(rule);
        var fileName = Path.GetFileName(rule.SourceFile);
        return Path.Combine(destinationDirectory ?? rule.DestinationRoot, fileName);
    }

    // Testable method - update rule after successful copy
    internal void UpdateRuleAfterSuccessfulCopy(Models.WatchRule rule)
    {
        _rulesService.UpdateRuleLastCopied(rule, _clock.UtcNow);
        // Status is already set in ProcessRuleAsync, not needed here
    }
}