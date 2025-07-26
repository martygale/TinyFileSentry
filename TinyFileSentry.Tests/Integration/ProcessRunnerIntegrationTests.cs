using TinyFileSentry.Core.Adapters;

namespace TinyFileSentry.Tests.Integration;

[TestFixture]
public class ProcessRunnerIntegrationTests
{
    private ProcessRunner _processRunner = null!;
    private string _tempGitRepoPath = null!;

    [SetUp]
    public void Setup()
    {
        _processRunner = new ProcessRunner();
        
        // Create temporary folder for git repository
        _tempGitRepoPath = Path.Combine(Path.GetTempPath(), $"git_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempGitRepoPath);
        
        // Initialize git repository
        var initResult = _processRunner.RunCommand("git", "init", _tempGitRepoPath);
        Assert.That(initResult.ExitCode, Is.EqualTo(0), $"Git init failed: {initResult.StdErr}");
        
        // Setup basic git configuration for tests
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
    public void RunCommand_GitInit_ReturnsSuccessExitCode()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"git_init_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPath);

        try
        {
            // Act
            var result = _processRunner.RunCommand("git", "init", tempPath);

            // Assert
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StdOut, Contains.Substring("Initialized empty Git repository"));
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }

    [Test]
    public void RunCommand_GitStatus_EmptyRepo_ReturnsCorrectOutput()
    {
        // Act
        var result = _processRunner.RunCommand("git", "status --porcelain", _tempGitRepoPath);

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut.Trim(), Is.Empty); // Empty repository without changes
    }

    [Test]
    public void RunCommand_GitAddAndCommit_WorksCorrectly()
    {
        // Arrange
        var testFilePath = Path.Combine(_tempGitRepoPath, "test.txt");
        File.WriteAllText(testFilePath, "Test content");

        // Act - add file
        var addResult = _processRunner.RunCommand("git", "add test.txt", _tempGitRepoPath);
        
        // Assert - verify that add was successful
        Assert.That(addResult.ExitCode, Is.EqualTo(0));
        
        // Act - make commit
        var commitResult = _processRunner.RunCommand("git", "commit -m \"Test commit\"", _tempGitRepoPath);
        
        // Assert - verify that commit was successful
        Assert.That(commitResult.ExitCode, Is.EqualTo(0));
        Assert.That(commitResult.StdOut, Contains.Substring("Test commit"));
    }

    [Test]
    public void RunCommand_GitAddNonExistentFile_ReturnsErrorExitCode()
    {
        // Act
        var result = _processRunner.RunCommand("git", "add nonexistent.txt", _tempGitRepoPath);

        // Assert
        Assert.That(result.ExitCode, Is.Not.EqualTo(0));
        Assert.That(result.StdErr, Contains.Substring("pathspec"));
    }

    [Test]
    public void RunCommand_InvalidCommand_ReturnsErrorExitCode()
    {
        // Act
        var result = _processRunner.RunCommand("git", "invalidcommand", _tempGitRepoPath);

        // Assert
        Assert.That(result.ExitCode, Is.Not.EqualTo(0));
        Assert.That(result.StdErr, Is.Not.Empty);
    }

    [Test]
    public void RunCommand_WithoutWorkingDirectory_UsesCurrentDirectory()
    {
        // Act
        var result = _processRunner.RunCommand("git", "--version");

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut, Contains.Substring("git version"));
    }

    [Test]
    public void RunCommand_NonExistentExecutable_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<System.ComponentModel.Win32Exception>(() =>
            _processRunner.RunCommand("nonexistentcommand", ""));
    }

    [Test]
    public void RunCommand_GitLog_EmptyRepo_ReturnsAppropriateMessage()
    {
        // Act
        var result = _processRunner.RunCommand("git", "log --oneline", _tempGitRepoPath);

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(128)); // Git returns 128 for empty repo log
        Assert.That(result.StdErr, Contains.Substring("does not have any commits"));
    }

    [Test]
    public void RunCommand_GitLog_WithCommits_ReturnsCommitHistory()
    {
        // Arrange - create and commit file
        var testFilePath = Path.Combine(_tempGitRepoPath, "history.txt");
        File.WriteAllText(testFilePath, "History test");
        
        _processRunner.RunCommand("git", "add history.txt", _tempGitRepoPath);
        _processRunner.RunCommand("git", "commit -m \"First commit\"", _tempGitRepoPath);

        // Act
        var result = _processRunner.RunCommand("git", "log --oneline", _tempGitRepoPath);

        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.StdOut, Contains.Substring("First commit"));
    }
}
