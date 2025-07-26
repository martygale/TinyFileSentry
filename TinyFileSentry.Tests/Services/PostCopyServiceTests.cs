using Moq;
using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Models;
using TinyFileSentry.Core.Services;

namespace TinyFileSentry.Tests.Services;

[TestFixture]
public class PostCopyServiceTests
{
    private Mock<IProcessRunner> _processRunnerMock = null!;
    private Mock<ILogService> _logServiceMock = null!;
    private Mock<IFileSystem> _fileSystemMock = null!;
    private PostCopyService _postCopyService = null!;

    [SetUp]
    public void Setup()
    {
        _processRunnerMock = new Mock<IProcessRunner>();
        _logServiceMock = new Mock<ILogService>();
        _fileSystemMock = new Mock<IFileSystem>();
        _postCopyService = new PostCopyService(_processRunnerMock.Object, _logServiceMock.Object, _fileSystemMock.Object);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithNone_ReturnsTrue()
    {
        var result = await _postCopyService.ExecutePostActionAsync(
            PostActionType.None, 
            "test.txt", 
            "/dest",
            "/source/test.txt");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithGitCommit_Success_ReturnsTrue()
    {
        _fileSystemMock.Setup(x => x.FileExists("/dest/test.txt")).Returns(true);
        _processRunnerMock.Setup(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });
        _processRunnerMock.Setup(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });

        var result = await _postCopyService.ExecutePostActionAsync(
            PostActionType.GitCommit, 
            "/dest/test.txt", 
            "/dest",
            "/source/test.txt");

        Assert.That(result, Is.True);
        _processRunnerMock.Verify(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"), Times.Once);
        _processRunnerMock.Verify(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"), Times.Once);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithGitCommit_AddFails_ReturnsFalse()
    {
        _fileSystemMock.Setup(x => x.FileExists("/dest/test.txt")).Returns(true);
        _processRunnerMock.Setup(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 1, StdErr = "Error adding file" });

        var result = await _postCopyService.ExecutePostActionAsync(
            PostActionType.GitCommit, 
            "/dest/test.txt", 
            "/dest",
            "/source/test.txt");

        Assert.That(result, Is.False);
        _processRunnerMock.Verify(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"), Times.Once);
        _processRunnerMock.Verify(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"), Times.Never);
        _logServiceMock.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithGitCommit_CommitFails_ReturnsFalse()
    {
        _fileSystemMock.Setup(x => x.FileExists("/dest/test.txt")).Returns(true);
        _processRunnerMock.Setup(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });
        _processRunnerMock.Setup(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 128, StdErr = "repository not found" });

        var result = await _postCopyService.ExecutePostActionAsync(
            PostActionType.GitCommit, 
            "/dest/test.txt", 
            "/dest",
            "/source/test.txt");

        Assert.That(result, Is.False);
        _logServiceMock.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithInvalidActionType_ReturnsFalse()
    {
        var result = await _postCopyService.ExecutePostActionAsync(
            (PostActionType)999, 
            "test.txt", 
            "/dest",
            "/source/test.txt");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithGitCommit_FileInSubfolder_UsesRelativePath()
    {
        // Prepare paths for test
        var filePath = Path.Combine("/dest", "subfolder", "test.txt");
        var destinationRoot = "/dest";
        
        // Set expectations for git commands with full file path
        _fileSystemMock.Setup(x => x.FileExists(filePath)).Returns(true);
        _processRunnerMock.Setup(x => x.RunCommand("git", $"add \"{filePath}\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });
        _processRunnerMock.Setup(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/subfolder/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });

        var result = await _postCopyService.ExecutePostActionAsync(
            PostActionType.GitCommit, 
            filePath,  // File in subfolder
            destinationRoot,
            "/source/subfolder/test.txt");

        Assert.That(result, Is.True);
        // Verify that git add is called with full file path
        _processRunnerMock.Verify(x => x.RunCommand("git", $"add \"{filePath}\"", "/dest"), Times.Once);
        _processRunnerMock.Verify(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/subfolder/test.txt\"", "/dest"), Times.Once);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithGitCommit_NestedSubfolders_UsesCorrectRelativePath()
    {
        // Prepare paths for test
        var filePath = Path.Combine("/dest", "folder1", "folder2", "document.pdf");
        var destinationRoot = "/dest";
        
        // Test for file in deeply nested folder structure
        _fileSystemMock.Setup(x => x.FileExists(filePath)).Returns(true);
        _processRunnerMock.Setup(x => x.RunCommand("git", $"add \"{filePath}\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });
        _processRunnerMock.Setup(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/folder1/folder2/document.pdf\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });

        var result = await _postCopyService.ExecutePostActionAsync(
            PostActionType.GitCommit, 
            filePath,  // File in deeply nested structure
            destinationRoot,
            "/source/folder1/folder2/document.pdf");

        Assert.That(result, Is.True);
        // Verify correct full path for nested structure
        _processRunnerMock.Verify(x => x.RunCommand("git", $"add \"{filePath}\"", "/dest"), Times.Once);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithGitCommit_FileWithSpaces_EscapesPathCorrectly()
    {
        // Prepare paths for test
        var filePath = Path.Combine("/dest", "My Documents", "important file.docx");
        var destinationRoot = "/dest";
        
        // Test for file with spaces in name and path
        _fileSystemMock.Setup(x => x.FileExists(filePath)).Returns(true);
        _processRunnerMock.Setup(x => x.RunCommand("git", $"add \"{filePath}\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });
        _processRunnerMock.Setup(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/My Documents/important file.docx\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });

        var result = await _postCopyService.ExecutePostActionAsync(
            PostActionType.GitCommit, 
            filePath,  // File with spaces
            destinationRoot,
            "/source/My Documents/important file.docx");

        Assert.That(result, Is.True);
        // Verify that path with spaces is properly escaped with quotes
        _processRunnerMock.Verify(x => x.RunCommand("git", $"add \"{filePath}\"", "/dest"), Times.Once);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithGitCommitAndPush_Success_ReturnsTrue()
    {
        _fileSystemMock.Setup(x => x.FileExists("/dest/test.txt")).Returns(true);
        _processRunnerMock.Setup(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });
        _processRunnerMock.Setup(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });
        _processRunnerMock.Setup(x => x.RunCommand("git", "push", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });

        var result = await _postCopyService.ExecutePostActionAsync(
            PostActionType.GitCommitAndPush, 
            "/dest/test.txt", 
            "/dest",
            "/source/test.txt");

        Assert.That(result, Is.True);
        _processRunnerMock.Verify(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"), Times.Once);
        _processRunnerMock.Verify(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"), Times.Once);
        _processRunnerMock.Verify(x => x.RunCommand("git", "push", "/dest"), Times.Once);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithGitCommitAndPush_AddFails_ReturnsFalse()
    {
        _fileSystemMock.Setup(x => x.FileExists("/dest/test.txt")).Returns(true);
        _processRunnerMock.Setup(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 1, StdErr = "Error adding file" });

        var result = await _postCopyService.ExecutePostActionAsync(
            PostActionType.GitCommitAndPush, 
            "/dest/test.txt", 
            "/dest",
            "/source/test.txt");

        Assert.That(result, Is.False);
        _processRunnerMock.Verify(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"), Times.Once);
        _processRunnerMock.Verify(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"), Times.Never);
        _processRunnerMock.Verify(x => x.RunCommand("git", "push", "/dest"), Times.Never);
        _logServiceMock.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithGitCommitAndPush_CommitFails_ReturnsFalse()
    {
        _fileSystemMock.Setup(x => x.FileExists("/dest/test.txt")).Returns(true);
        _processRunnerMock.Setup(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });
        _processRunnerMock.Setup(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 128, StdErr = "repository not found" });

        var result = await _postCopyService.ExecutePostActionAsync(
            PostActionType.GitCommitAndPush, 
            "/dest/test.txt", 
            "/dest",
            "/source/test.txt");

        Assert.That(result, Is.False);
        _processRunnerMock.Verify(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"), Times.Once);
        _processRunnerMock.Verify(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"), Times.Once);
        _processRunnerMock.Verify(x => x.RunCommand("git", "push", "/dest"), Times.Never);
        _logServiceMock.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithGitCommitAndPush_PushFails_ReturnsFalse()
    {
        _fileSystemMock.Setup(x => x.FileExists("/dest/test.txt")).Returns(true);
        _processRunnerMock.Setup(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });
        _processRunnerMock.Setup(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });
        _processRunnerMock.Setup(x => x.RunCommand("git", "push", "/dest"))
            .Returns(new ProcessResult { ExitCode = 1, StdErr = "Push failed" });

        var result = await _postCopyService.ExecutePostActionAsync(
            PostActionType.GitCommitAndPush, 
            "/dest/test.txt", 
            "/dest",
            "/source/test.txt");

        Assert.That(result, Is.False);
        _processRunnerMock.Verify(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"), Times.Once);
        _processRunnerMock.Verify(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"), Times.Once);
        _processRunnerMock.Verify(x => x.RunCommand("git", "push", "/dest"), Times.Once);
        _logServiceMock.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ExecutePostActionAsync_WithGitCommitAndPush_NothingToCommit_ReturnsTrue()
    {
        _fileSystemMock.Setup(x => x.FileExists("/dest/test.txt")).Returns(true);
        _processRunnerMock.Setup(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 0 });
        _processRunnerMock.Setup(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"))
            .Returns(new ProcessResult { ExitCode = 1, StdOut = "nothing to commit, working tree clean" });

        var result = await _postCopyService.ExecutePostActionAsync(
            PostActionType.GitCommitAndPush, 
            "/dest/test.txt", 
            "/dest",
            "/source/test.txt");

        Assert.That(result, Is.True);
        _processRunnerMock.Verify(x => x.RunCommand("git", "add \"/dest/test.txt\"", "/dest"), Times.Once);
        _processRunnerMock.Verify(x => x.RunCommand("git", "commit -m \"Auto-backup from /source/test.txt\"", "/dest"), Times.Once);
        _processRunnerMock.Verify(x => x.RunCommand("git", "push", "/dest"), Times.Never); // Should not push if nothing to commit
        _logServiceMock.Verify(x => x.Info(It.Is<string>(s => s.Contains("skipped - no changes to commit")), It.IsAny<string>()), Times.Once);
    }
}