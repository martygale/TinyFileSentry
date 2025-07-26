using TinyFileSentry.Core.Interfaces;

namespace TinyFileSentry.Core.Services.PostActions;

public class GitCommitAndPushPostAction : IPostAction
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogService _logService;
    private readonly IFileSystem _fileSystem;

    public GitCommitAndPushPostAction(IProcessRunner processRunner, ILogService logService, IFileSystem fileSystem)
    {
        _processRunner = processRunner;
        _logService = logService;
        _fileSystem = fileSystem;
    }

    public Task<bool> ExecuteAsync(string filePath, string destinationDirectory, string sourceFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // Calculate relative file path from destination directory
            var relativePath = Path.GetRelativePath(destinationDirectory, filePath);
            
            // Log paths for diagnostics
            _logService.Info($"Git commit and push operation paths - File: {filePath}, Destination: {destinationDirectory}, Relative: {relativePath}", nameof(GitCommitAndPushPostAction));
            
            // Check if file exists before git add
            if (!_fileSystem.FileExists(filePath))
            {
                _logService.Error($"File does not exist: {filePath}", nameof(GitCommitAndPushPostAction));
                return Task.FromResult(false);
            }
            
            // Step 1: Git add
            var addResult = _processRunner.RunCommand("git", $"add \"{filePath}\"", destinationDirectory);
            
            if (addResult.ExitCode != 0)
            {
                var addErrorDetails = !string.IsNullOrEmpty(addResult.StdErr) ? addResult.StdErr : addResult.StdOut;
                _logService.Error($"Git add failed with exit code {addResult.ExitCode}: {addErrorDetails}", nameof(GitCommitAndPushPostAction));
                return Task.FromResult(false);
            }

            // Step 2: Git commit
            var commitResult = _processRunner.RunCommand("git", $"commit -m \"Auto-backup from {sourceFilePath}\"", destinationDirectory);
            
            if (commitResult.ExitCode != 0)
            {
                var commitErrorDetails = !string.IsNullOrEmpty(commitResult.StdErr) ? commitResult.StdErr : commitResult.StdOut;
                
                // Check if this is "nothing to commit" error - in this case skip push and consider operation successful
                if (commitErrorDetails.Contains("nothing to commit") || commitErrorDetails.Contains("working tree clean"))
                {
                    _logService.Info($"Git commit skipped - no changes to commit for {relativePath}", nameof(GitCommitAndPushPostAction));
                    return Task.FromResult(true);
                }
                
                _logService.Error($"Git commit failed with exit code {commitResult.ExitCode}: {commitErrorDetails}", nameof(GitCommitAndPushPostAction));
                return Task.FromResult(false);
            }

            _logService.Info($"Git commit completed successfully for {relativePath}", nameof(GitCommitAndPushPostAction));

            // Step 3: Git push
            var pushResult = _processRunner.RunCommand("git", "push", destinationDirectory);
            
            if (pushResult.ExitCode != 0)
            {
                var pushErrorDetails = !string.IsNullOrEmpty(pushResult.StdErr) ? pushResult.StdErr : pushResult.StdOut;
                _logService.Error($"Git push failed with exit code {pushResult.ExitCode}: {pushErrorDetails}", nameof(GitCommitAndPushPostAction));
                return Task.FromResult(false);
            }

            _logService.Info($"Git commit and push completed successfully for {relativePath}", nameof(GitCommitAndPushPostAction));
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logService.Error($"Git commit and push failed with exception: {ex.Message}", nameof(GitCommitAndPushPostAction));
            return Task.FromResult(false);
        }
    }
}