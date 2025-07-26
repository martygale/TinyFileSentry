using TinyFileSentry.Core.Adapters;
using TinyFileSentry.Core.Services;
using TinyFileSentry.Core.Services.PostActions;

namespace TinyFileSentry.Tests.Integration;

[TestFixture]
public class GitCommitPostActionIntegrationTests
{
    private ProcessRunner _processRunner = null!;
    private LogService _logService = null!;
    private GitCommitPostAction _gitCommitPostAction = null!;
    private string _tempGitRepoPath = null!;

    [SetUp]
    public void Setup()
    {
        _processRunner = new ProcessRunner();
        _logService = new LogService(new SystemClock());
        var fileSystem = new FileSystemAdapter();
        _gitCommitPostAction = new GitCommitPostAction(_processRunner, _logService, fileSystem);
        
        // Create temporary folder for git repository
        _tempGitRepoPath = Path.Combine(Path.GetTempPath(), $"git_postaction_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempGitRepoPath);
        
        // Initialize git repository
        var initResult = _processRunner.RunCommand("git", "init", _tempGitRepoPath);
        Assert.That(initResult.ExitCode, Is.EqualTo(0), $"Git init failed: {initResult.StdErr}");
        
        // Configure basic git settings for tests
        _processRunner.RunCommand("git", "config user.name \"Test User\"", _tempGitRepoPath);
        _processRunner.RunCommand("git", "config user.email \"test@example.com\"", _tempGitRepoPath);
    }

    [TearDown]
    public void TearDown()
    {
        // Delete temporary folder after each test
        if (Directory.Exists(_tempGitRepoPath))
        {
            // Force remove read-only attributes before deletion (for Windows)
            RemoveReadOnlyAttributes(_tempGitRepoPath);
            Directory.Delete(_tempGitRepoPath, true);
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
    public async Task ExecuteAsync_WithValidFile_ReturnsTrue()
    {
        // Arrange
        var testFilePath = Path.Combine(_tempGitRepoPath, "test.txt");
        File.WriteAllText(testFilePath, "Test content");

        // Act
        var result = await _gitCommitPostAction.ExecuteAsync(testFilePath, _tempGitRepoPath, "/source/test.txt");

        // Assert
        Assert.That(result, Is.True);
        
        // Verify that file was actually committed
        var logResult = _processRunner.RunCommand("git", "log --oneline", _tempGitRepoPath);
        Assert.That(logResult.ExitCode, Is.EqualTo(0));
        Assert.That(logResult.StdOut, Contains.Substring("Auto-backup"));
    }

    [Test]
    public async Task ExecuteAsync_WithFileInSubfolder_ReturnsTrue()
    {
        // Arrange
        var subfolderPath = Path.Combine(_tempGitRepoPath, "subfolder");
        Directory.CreateDirectory(subfolderPath);
        var testFilePath = Path.Combine(subfolderPath, "test.txt");
        File.WriteAllText(testFilePath, "Test content in subfolder");

        // Act
        var result = await _gitCommitPostAction.ExecuteAsync(testFilePath, _tempGitRepoPath, "/source/test.txt");

        // Assert
        Assert.That(result, Is.True);
        
        // Verify that file was actually committed
        var logResult = _processRunner.RunCommand("git", "log --oneline", _tempGitRepoPath);
        Assert.That(logResult.ExitCode, Is.EqualTo(0));
        Assert.That(logResult.StdOut, Contains.Substring("Auto-backup"));
        
        // Verify git status - should be clean
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
        var result = await _gitCommitPostAction.ExecuteAsync(nonExistentFilePath, _tempGitRepoPath, "/source/nonexistent.txt");

        // Assert
        Assert.That(result, Is.False);
        
        // Verify that error was logged
        var logEntries = _logService.GetLogs();
        Assert.That(logEntries.Any(entry => 
            entry.Message.Contains("File does not exist") && 
            entry.Message.Contains(nonExistentFilePath)), Is.True);
    }

    [Test]
    public async Task ExecuteAsync_WithFileWithSpaces_ReturnsTrue()
    {
        // Arrange
        var testFilePath = Path.Combine(_tempGitRepoPath, "file with spaces.txt");
        File.WriteAllText(testFilePath, "Test content with spaces in filename");

        // Act
        var result = await _gitCommitPostAction.ExecuteAsync(testFilePath, _tempGitRepoPath, "/source/test.txt");

        // Assert
        Assert.That(result, Is.True);
        
        // Verify that file was actually committed
        var logResult = _processRunner.RunCommand("git", "log --oneline", _tempGitRepoPath);
        Assert.That(logResult.ExitCode, Is.EqualTo(0));
        Assert.That(logResult.StdOut, Contains.Substring("Auto-backup"));
    }

    [Test]
    public async Task ExecuteAsync_WithAlreadyCommittedFile_ReturnsTrue()
    {
        // Arrange
        var testFilePath = Path.Combine(_tempGitRepoPath, "already_committed.txt");
        File.WriteAllText(testFilePath, "Initial content");
        
        // First commit
        _processRunner.RunCommand("git", $"add \"{testFilePath}\"", _tempGitRepoPath);
        _processRunner.RunCommand("git", "commit -m \"Initial commit\"", _tempGitRepoPath);
        
        // Modify file
        File.WriteAllText(testFilePath, "Modified content");

        // Act
        var result = await _gitCommitPostAction.ExecuteAsync(testFilePath, _tempGitRepoPath, "/source/test.txt");

        // Assert
        Assert.That(result, Is.True);
        
        // Verify that there are two commits
        var logResult = _processRunner.RunCommand("git", "log --oneline", _tempGitRepoPath);
        Assert.That(logResult.ExitCode, Is.EqualTo(0));
        var commitLines = logResult.StdOut.Trim().Split('\n');
        Assert.That(commitLines.Length, Is.EqualTo(2));
        Assert.That(logResult.StdOut, Contains.Substring("Auto-backup"));
        Assert.That(logResult.StdOut, Contains.Substring("Initial commit"));
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
            var result = await _gitCommitPostAction.ExecuteAsync(testFilePath, nonGitPath, "/source/test.txt");

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
    public async Task ExecuteAsync_LogsCorrectPaths()
    {
        // Arrange
        var testFilePath = Path.Combine(_tempGitRepoPath, "log_test.txt");
        File.WriteAllText(testFilePath, "Test content for logging");

        // Act
        var result = await _gitCommitPostAction.ExecuteAsync(testFilePath, _tempGitRepoPath, "/source/test.txt");

        // Assert
        Assert.That(result, Is.True);
        
        // Verify path logging
        var logEntries = _logService.GetLogs();
        var pathLogEntry = logEntries.FirstOrDefault(entry => 
            entry.Message.Contains("Git operation paths"));
        
        Assert.That(pathLogEntry, Is.Not.Null);
        Assert.That(pathLogEntry.Message, Contains.Substring($"File: {testFilePath}"));
        Assert.That(pathLogEntry.Message, Contains.Substring($"Destination: {_tempGitRepoPath}"));
        
        // Verify successful completion
        var successLogEntry = logEntries.FirstOrDefault(entry => 
            entry.Message.Contains("Git commit completed successfully"));
        Assert.That(successLogEntry, Is.Not.Null);
    }

    [Test]
    public async Task ExecuteAsync_WithDeepNestedPath_ReturnsTrue()
    {
        // Arrange
        var deepPath = Path.Combine(_tempGitRepoPath, "level1", "level2", "level3");
        Directory.CreateDirectory(deepPath);
        var testFilePath = Path.Combine(deepPath, "deep_file.txt");
        File.WriteAllText(testFilePath, "Content in deeply nested structure");

        // Act
        var result = await _gitCommitPostAction.ExecuteAsync(testFilePath, _tempGitRepoPath, "/source/test.txt");

        // Assert
        Assert.That(result, Is.True);
        
        // Verify that file was actually committed
        var logResult = _processRunner.RunCommand("git", "log --oneline", _tempGitRepoPath);
        Assert.That(logResult.ExitCode, Is.EqualTo(0));
        Assert.That(logResult.StdOut, Contains.Substring("Auto-backup"));
        
        // Verify that file is tracked by git
        var lsFilesResult = _processRunner.RunCommand("git", "ls-files", _tempGitRepoPath);
        Assert.That(lsFilesResult.ExitCode, Is.EqualTo(0));
        Assert.That(lsFilesResult.StdOut, Contains.Substring("deep_file.txt"));
    }
}