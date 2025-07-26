using TinyFileSentry.Core.Interfaces;

namespace TinyFileSentry.Core.Services.PostActions;

public class GitCommitPostAction : IPostAction
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogService _logService;
    private readonly IFileSystem _fileSystem;

    public GitCommitPostAction(IProcessRunner processRunner, ILogService logService, IFileSystem fileSystem)
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
            _logService.Info($"Git operation paths - File: {filePath}, Destination: {destinationDirectory}, Relative: {relativePath}", nameof(GitCommitPostAction));
            
            // Check if file exists before git add
            if (!_fileSystem.FileExists(filePath))
            {
                _logService.Error($"File does not exist: {filePath}", nameof(GitCommitPostAction));
                return Task.FromResult(false);
            }
            
            // Try using full file path instead of relative path
            var addResult = _processRunner.RunCommand("git", $"add \"{filePath}\"", destinationDirectory);
            
            if (addResult.ExitCode != 0)
            {
                var addErrorDetails = !string.IsNullOrEmpty(addResult.StdErr) ? addResult.StdErr : addResult.StdOut;
                _logService.Error($"Git add failed with exit code {addResult.ExitCode}: {addErrorDetails}", nameof(GitCommitPostAction));
                return Task.FromResult(false);
            }

            var commitResult = _processRunner.RunCommand("git", $"commit -m \"Auto-backup from {sourceFilePath}\"", destinationDirectory);
            
            if (commitResult.ExitCode != 0)
            {
                var commitErrorDetails = !string.IsNullOrEmpty(commitResult.StdErr) ? commitResult.StdErr : commitResult.StdOut;
                
                // Check if this is "nothing to commit" error - in this case consider operation successful
                if (commitErrorDetails.Contains("nothing to commit") || commitErrorDetails.Contains("working tree clean"))
                {
                    _logService.Info($"Git commit skipped - no changes to commit for {relativePath}", nameof(GitCommitPostAction));
                    return Task.FromResult(true);
                }
                
                _logService.Error($"Git commit failed with exit code {commitResult.ExitCode}: {commitErrorDetails}", nameof(GitCommitPostAction));
                return Task.FromResult(false);
            }

            _logService.Info($"Git commit completed successfully for {relativePath}", nameof(GitCommitPostAction));
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logService.Error($"Git commit failed with exception: {ex.Message}", nameof(GitCommitPostAction));
            return Task.FromResult(false);
        }
    }
}