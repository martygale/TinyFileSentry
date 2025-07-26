using TinyFileSentry.Core.Adapters;
using TinyFileSentry.Core.Services;
using TinyFileSentry.Core.Services.PostActions;

namespace TinyFileSentry.Tests.Integration;

[TestFixture]
public class GitCommitAndPushPostActionIntegrationTests
{
    private ProcessRunner _processRunner = null!;
    private LogService _logService = null!;
    private GitCommitAndPushPostAction _gitCommitAndPushPostAction = null!;
    private string _tempGitRepoPath = null!;
    private string _tempRemotePath = null!;

    [SetUp]
    public void Setup()
    {
        _processRunner = new ProcessRunner();
        _logService = new LogService(new SystemClock());
        var fileSystem = new FileSystemAdapter();
        _gitCommitAndPushPostAction = new GitCommitAndPushPostAction(_processRunner, _logService, fileSystem);
        
        // Create temporary folder for git repository
        _tempGitRepoPath = Path.Combine(Path.GetTempPath(), $"git_commit_push_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempGitRepoPath);
        
        // Create temporary "remote" repository
        _tempRemotePath = Path.Combine(Path.GetTempPath(), $"git_remote_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRemotePath);
        
        // Initialize bare remote repository
        var initRemoteResult = _processRunner.RunCommand("git", "init --bare", _tempRemotePath);
        Assert.That(initRemoteResult.ExitCode, Is.EqualTo(0), $"Git init --bare failed: {initRemoteResult.StdErr}");
        
        // Initialize local git repository
        var initResult = _processRunner.RunCommand("git", "init", _tempGitRepoPath);
        Assert.That(initResult.ExitCode, Is.EqualTo(0), $"Git init failed: {initResult.StdErr}");
        
        // Configure basic git settings for tests
        _processRunner.RunCommand("git", "config user.name \"Test User\"", _tempGitRepoPath);
        _processRunner.RunCommand("git", "config user.email \"test@example.com\"", _tempGitRepoPath);
        
        // Add remote origin
        var addRemoteResult = _processRunner.RunCommand("git", $"remote add origin \"{_tempRemotePath}\"", _tempGitRepoPath);
        Assert.That(addRemoteResult.ExitCode, Is.EqualTo(0), $"Git remote add failed: {addRemoteResult.StdErr}");
        
        // Set default branch name to main
        _processRunner.RunCommand("git", "config init.defaultBranch main", _tempGitRepoPath);
        
        // Create initial commit and push to establish upstream
        var initialFilePath = Path.Combine(_tempGitRepoPath, "initial.txt");
        File.WriteAllText(initialFilePath, "Initial commit");
        _processRunner.RunCommand("git", "add initial.txt", _tempGitRepoPath);
        var commitResult = _processRunner.RunCommand("git", "commit -m \"Initial commit\"", _tempGitRepoPath);
        Assert.That(commitResult.ExitCode, Is.EqualTo(0), $"Initial commit failed: {commitResult.StdErr}");
        
        // Check current branch and rename if needed
        var branchResult = _processRunner.RunCommand("git", "branch --show-current", _tempGitRepoPath);
        if (branchResult.ExitCode == 0 && branchResult.StdOut.Trim() != "main")
        {
            _processRunner.RunCommand("git", "branch -M main", _tempGitRepoPath);
        }
        
        var pushResult = _processRunner.RunCommand("git", "push -u origin main", _tempGitRepoPath);
        Assert.That(pushResult.ExitCode, Is.EqualTo(0), $"Initial push failed: {pushResult.StdErr}");
    }

    [TearDown]
    public void TearDown()
    {
        // Delete temporary folders after each test
        if (Directory.Exists(_tempGitRepoPath))
        {
            RemoveReadOnlyAttributes(_tempGitRepoPath);
            Directory.Delete(_tempGitRepoPath, true);
        }
        
        if (Directory.Exists(_tempRemotePath))
        {
            RemoveReadOnlyAttributes(_tempRemotePath);
            Directory.Delete(_tempRemotePath, true);
        }
    }
    
    private static void RemoveReadOnlyAttributes(string directoryPath)
    {
        var directoryInfo = new DirectoryInfo(directoryPath);
        
        // Remove read-only attributes from folder
        if (directoryInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
        {
            directoryInfo.Attributes &= ~FileAttributes.ReadOnly;
        }
        
        // Recursively process all files and folders
        foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
        {
            if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                file.Attributes &= ~FileAttributes.ReadOnly;
            }
        }
        
        foreach (var subDir in directoryInfo.GetDirectories("*", SearchOption.AllDirectories))
        {
            if (subDir.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                subDir.Attributes &= ~FileAttributes.ReadOnly;
            }
        }
    }

    [Test]
    public async Task ExecuteAsync_WithValidFile_CommitsAndPushes()
    {
        // Arrange
        var testFilePath = Path.Combine(_tempGitRepoPath, "test.txt");
        File.WriteAllText(testFilePath, "Test content");

        // Act
        var result = await _gitCommitAndPushPostAction.ExecuteAsync(testFilePath, _tempGitRepoPath, "/source/test.txt");

        // Assert
        Assert.That(result, Is.True);
        
        // Verify that file was committed locally
        var logResult = _processRunner.RunCommand("git", "log --oneline", _tempGitRepoPath);
        Assert.That(logResult.ExitCode, Is.EqualTo(0));
        Assert.That(logResult.StdOut, Contains.Substring("Auto-backup"));
        
        // Verify that changes were pushed to remote
        var statusResult = _processRunner.RunCommand("git", "status", _tempGitRepoPath);
        Assert.That(statusResult.ExitCode, Is.EqualTo(0));
        Assert.That(statusResult.StdOut, Contains.Substring("Your branch is up to date"));
    }

    [Test]
    public async Task ExecuteAsync_WithFileInSubfolder_CommitsAndPushes()
    {
        // Arrange
        var subfolderPath = Path.Combine(_tempGitRepoPath, "subfolder");
        Directory.CreateDirectory(subfolderPath);
        var testFilePath = Path.Combine(subfolderPath, "test.txt");
        File.WriteAllText(testFilePath, "Test content in subfolder");

        // Act
        var result = await _gitCommitAndPushPostAction.ExecuteAsync(testFilePath, _tempGitRepoPath, "/source/test.txt");

        // Assert
        Assert.That(result, Is.True);
        
        // Verify that file was committed and pushed
        var logResult = _processRunner.RunCommand("git", "log --oneline", _tempGitRepoPath);
        Assert.That(logResult.ExitCode, Is.EqualTo(0));
        Assert.That(logResult.StdOut, Contains.Substring("Auto-backup"));
        
        // Verify git status - should be clean and up to date
        var statusResult = _processRunner.RunCommand("git", "status --porcelain", _tempGitRepoPath);
        Assert.That(statusResult.ExitCode, Is.EqualTo(0));
        Assert.That(statusResult.StdOut.Trim(), Is.Empty);
    }

    [Test]
    public async Task ExecuteAsync_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentFilePath = Path.Combine(_tempGitRepoPath, "nonexistent.txt");

        // Act
        var result = await _gitCommitAndPushPostAction.ExecuteAsync(nonExistentFilePath, _tempGitRepoPath, "/source/nonexistent.txt");

        // Assert
        Assert.That(result, Is.False);
        
        // Verify that error was logged
        var logEntries = _logService.GetLogs();
        Assert.That(logEntries.Any(entry => 
            entry.Message.Contains("File does not exist") && 
            entry.Message.Contains(nonExistentFilePath)), Is.True);
    }

    [Test]
    public async Task ExecuteAsync_WithNoChangesToCommit_ReturnsTrue()
    {
        // Arrange
        var testFilePath = Path.Combine(_tempGitRepoPath, "existing.txt");
        File.WriteAllText(testFilePath, "Existing content");
        
        // First commit and push
        _processRunner.RunCommand("git", $"add \"{testFilePath}\"", _tempGitRepoPath);
        _processRunner.RunCommand("git", "commit -m \"Add existing file\"", _tempGitRepoPath);
        _processRunner.RunCommand("git", "push", _tempGitRepoPath);

        // Act - try to commit the same file again without changes
        var result = await _gitCommitAndPushPostAction.ExecuteAsync(testFilePath, _tempGitRepoPath, "/source/test.txt");

        // Assert
        Assert.That(result, Is.True);
        
        // Verify that "nothing to commit" message was logged
        var logEntries = _logService.GetLogs();
        Assert.That(logEntries.Any(entry => 
            entry.Message.Contains("skipped - no changes to commit")), Is.True);
    }

    [Test]
    public async Task ExecuteAsync_InNonGitDirectory_ReturnsFalse()
    {
        // Arrange
        var nonGitPath = Path.Combine(Path.GetTempPath(), $"non_git_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(nonGitPath);
        var testFilePath = Path.Combine(nonGitPath, "test.txt");
        File.WriteAllText(testFilePath, "Test content");

        try
        {
            // Act
            var result = await _gitCommitAndPushPostAction.ExecuteAsync(testFilePath, nonGitPath, "/source/test.txt");

            // Assert
            Assert.That(result, Is.False);
            
            // Verify that error was logged
            var logEntries = _logService.GetLogs();
            Assert.That(logEntries.Any(entry => 
                entry.Message.Contains("Git add failed") || 
                entry.Message.Contains("Git commit failed")), Is.True);
        }
        finally
        {
            if (Directory.Exists(nonGitPath))
                Directory.Delete(nonGitPath, true);
        }
    }

    [Test]
    public async Task ExecuteAsync_LogsCorrectMessages()
    {
        // Arrange
        var testFilePath = Path.Combine(_tempGitRepoPath, "log_test.txt");
        File.WriteAllText(testFilePath, "Test content for logging");

        // Act
        var result = await _gitCommitAndPushPostAction.ExecuteAsync(testFilePath, _tempGitRepoPath, "/source/test.txt");

        // Assert
        Assert.That(result, Is.True);
        
        // Verify path logging
        var logEntries = _logService.GetLogs();
        var pathLogEntry = logEntries.FirstOrDefault(entry => 
            entry.Message.Contains("Git commit and push operation paths"));
        
        Assert.That(pathLogEntry, Is.Not.Null);
        Assert.That(pathLogEntry.Message, Contains.Substring($"File: {testFilePath}"));
        Assert.That(pathLogEntry.Message, Contains.Substring($"Destination: {_tempGitRepoPath}"));
        
        // Verify successful commit message
        var commitSuccessEntry = logEntries.FirstOrDefault(entry => 
            entry.Message.Contains("Git commit completed successfully"));
        Assert.That(commitSuccessEntry, Is.Not.Null);
        
        // Verify successful push message
        var pushSuccessEntry = logEntries.FirstOrDefault(entry => 
            entry.Message.Contains("Git commit and push completed successfully"));
        Assert.That(pushSuccessEntry, Is.Not.Null);
    }

    [Test]
    public async Task ExecuteAsync_WithFileWithSpaces_CommitsAndPushes()
    {
        // Arrange
        var testFilePath = Path.Combine(_tempGitRepoPath, "file with spaces.txt");
        File.WriteAllText(testFilePath, "Test content with spaces in filename");

        // Act
        var result = await _gitCommitAndPushPostAction.ExecuteAsync(testFilePath, _tempGitRepoPath, "/source/test.txt");

        // Assert
        Assert.That(result, Is.True);
        
        // Verify that file was committed and pushed
        var logResult = _processRunner.RunCommand("git", "log --oneline", _tempGitRepoPath);
        Assert.That(logResult.ExitCode, Is.EqualTo(0));
        Assert.That(logResult.StdOut, Contains.Substring("Auto-backup"));
        
        // Verify working tree is clean
        var statusResult = _processRunner.RunCommand("git", "status --porcelain", _tempGitRepoPath);
        Assert.That(statusResult.ExitCode, Is.EqualTo(0));
        Assert.That(statusResult.StdOut.Trim(), Is.Empty);
    }
}