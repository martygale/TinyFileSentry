using NUnit.Framework;
using Moq;
using TinyFileSentry.Core.Services;
using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Models;

namespace TinyFileSentry.Tests.Services;

[TestFixture]
public class PollerServiceTests
{
    private PollerService _pollerService = null!;
    private Mock<IRulesService> _mockRulesService = null!;
    private Mock<IFileSystem> _mockFileSystem = null!;
    private Mock<IHashService> _mockHashService = null!;
    private Mock<ICopyService> _mockCopyService = null!;
    private Mock<IPostCopyService> _mockPostCopyService = null!;
    private Mock<ILogService> _mockLogService = null!;
    private Mock<IClock> _mockClock = null!;
    private Mock<IConfigurationService> _mockConfigurationService = null!;
    private Mock<IPathSanitizer> _mockPathSanitizer = null!;

    [SetUp]
    public void Setup()
    {
        _mockRulesService = new Mock<IRulesService>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockHashService = new Mock<IHashService>();
        _mockCopyService = new Mock<ICopyService>();
        _mockPostCopyService = new Mock<IPostCopyService>();
        _mockLogService = new Mock<ILogService>();
        _mockClock = new Mock<IClock>();
        _mockConfigurationService = new Mock<IConfigurationService>();
        _mockPathSanitizer = new Mock<IPathSanitizer>();

        _pollerService = new PollerService(
            _mockRulesService.Object,
            _mockFileSystem.Object,
            _mockHashService.Object,
            _mockCopyService.Object,
            _mockPostCopyService.Object,
            _mockLogService.Object,
            _mockClock.Object,
            _mockConfigurationService.Object,
            _mockPathSanitizer.Object
        );
    }

    [Test]
    public void ShouldProcessRule_WhenFileExists_ReturnsTrue()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\test\file.txt" 
        };
        _mockFileSystem.Setup(x => x.FileExists(rule.SourceFile)).Returns(true);

        // Act
        var result = _pollerService.ShouldProcessRule(rule);

        // Assert
        Assert.That(result, Is.True);
        _mockFileSystem.Verify(x => x.FileExists(rule.SourceFile), Times.Once);
    }

    [Test]
    public void ShouldProcessRule_WhenFileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\test\nonexistent.txt" 
        };
        _mockFileSystem.Setup(x => x.FileExists(rule.SourceFile)).Returns(false);

        // Act
        var result = _pollerService.ShouldProcessRule(rule);

        // Assert
        Assert.That(result, Is.False);
        _mockFileSystem.Verify(x => x.FileExists(rule.SourceFile), Times.Once);
    }

    [Test]
    public void HasFileChanged_WhenHashesDiffer_ReturnsTrue()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\test\file.txt",
            DestinationRoot = @"D:\backup"
        };
        
        // Mock hashes for source and destination to be different
        _mockHashService.Setup(x => x.ComputeHash(rule.SourceFile)).Returns("new_hash_456");
        var destPath = Path.Combine(rule.DestinationRoot, Path.GetFileName(rule.SourceFile));
        _mockFileSystem.Setup(x => x.FileExists(destPath)).Returns(true);
        _mockHashService.Setup(x => x.ComputeHash(destPath)).Returns("old_hash_123");

        // Act
        var result = _pollerService.HasFileChanged(rule);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void HasFileChanged_WhenHashesMatch_ReturnsFalse()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\test\file.txt",
            DestinationRoot = @"D:\backup"
        };
        
        // Mock same hashes for source and destination
        _mockHashService.Setup(x => x.ComputeHash(rule.SourceFile)).Returns("same_hash_123");
        var destPath = Path.Combine(rule.DestinationRoot, Path.GetFileName(rule.SourceFile));
        _mockFileSystem.Setup(x => x.FileExists(destPath)).Returns(true);
        _mockHashService.Setup(x => x.ComputeHash(destPath)).Returns("same_hash_123");

        // Act
        var result = _pollerService.HasFileChanged(rule);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void BuildDestinationPath_ValidRule_ReturnsCorrectPath()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"file.txt", // File without directory
            DestinationRoot = @"D:\Backup" 
        };
        var sanitizedPath = @"";
        _mockPathSanitizer.Setup(x => x.SanitizePath(@"")).Returns(sanitizedPath);

        // Act
        var result = _pollerService.BuildDestinationPath(rule);

        // Assert
        Assert.That(result, Is.EqualTo(@"D:\Backup"));
        _mockPathSanitizer.Verify(x => x.SanitizePath(@""), Times.Once);
    }

    [Test]
    public void BuildDestinationPath_FileInRoot_ReturnsCorrectPath()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\file.txt",
            DestinationRoot = @"D:\Backup" 
        };
        var sanitizedPath = @"c_";
        // On Windows Path.GetDirectoryName(@"C:\file.txt") returns "C:\", not ""
        var expectedDirectory = Path.GetDirectoryName(@"C:\file.txt") ?? string.Empty;
        _mockPathSanitizer.Setup(x => x.SanitizePath(expectedDirectory)).Returns(sanitizedPath);

        // Act
        var result = _pollerService.BuildDestinationPath(rule);

        // Assert
        Assert.That(result, Is.EqualTo(@"D:\Backup"));
        _mockPathSanitizer.Verify(x => x.SanitizePath(expectedDirectory), Times.Once);
    }

    [Test]
    public void UpdateRuleAfterSuccessfulCopy_DoesNothing()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\test\file.txt" 
        };

        // Act
        _pollerService.UpdateRuleAfterSuccessfulCopy(rule);

        // Assert
        // Method should do nothing as LastCopied tracking was removed
        _mockRulesService.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ExecutePostCopyActionsAsync_ValidPath_ExecutesPostAction()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\Work\file.txt",
            DestinationRoot = @"D:\Backup",
            PostAction = PostActionType.GitCommit
        };
        var currentTime = DateTime.UtcNow;

        _mockPathSanitizer.Setup(x => x.SanitizePath(@"C:\Work")).Returns(@"C__Work");
        _mockPostCopyService.Setup(x => x.ExecutePostActionAsync(
            PostActionType.GitCommit, 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockClock.Setup(x => x.UtcNow).Returns(currentTime);

        // Act
        await _pollerService.ExecutePostCopyActionsAsync(rule, CancellationToken.None);

        // Assert
        _mockPostCopyService.Verify(x => x.ExecutePostActionAsync(
            PostActionType.GitCommit, 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
        // UpdateRuleLastCopied is no longer used as LastCopied tracking was removed
    }

    [Test]
    public async Task ExecutePostCopyActionsAsync_PostActionFails_DoesNotUpdateRule()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\Work\file.txt",
            DestinationRoot = @"D:\Backup",
            PostAction = PostActionType.None
        };

        _mockPathSanitizer.Setup(x => x.SanitizePath(@"C:\Work")).Returns(@"C__Work");
        _mockPostCopyService.Setup(x => x.ExecutePostActionAsync(
            It.IsAny<PostActionType>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _pollerService.ExecutePostCopyActionsAsync(rule, CancellationToken.None);

        // Assert
        // UpdateRuleLastCopied is no longer used as LastCopied tracking was removed
    }

    [Test]
    public async Task ProcessAllRulesAsync_WithMultipleRules_ProcessesAllRules()
    {
        // Arrange
        var rules = new List<WatchRule>
        {
            new() { SourceFile = @"C:\file1.txt" },
            new() { SourceFile = @"C:\file2.txt" }
        };
        _mockRulesService.Setup(x => x.GetWatchRules()).Returns(rules);
        _mockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);

        // Act
        await _pollerService.ProcessAllRulesAsync();

        // Assert
        _mockRulesService.Verify(x => x.GetWatchRules(), Times.Once);
        _mockFileSystem.Verify(x => x.FileExists(@"C:\file1.txt"), Times.Once);
        _mockFileSystem.Verify(x => x.FileExists(@"C:\file2.txt"), Times.Once);
    }

    [Test]
    public async Task ProcessRuleAsync_FileDoesNotExist_DoesNotProcessRule()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\nonexistent.txt" 
        };
        _mockFileSystem.Setup(x => x.FileExists(rule.SourceFile)).Returns(false);

        // Act
        await _pollerService.ProcessRuleAsync(rule);

        // Assert
        _mockFileSystem.Verify(x => x.FileExists(rule.SourceFile), Times.Once);
        _mockHashService.Verify(x => x.ComputeHash(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ProcessRuleAsync_FileUnchanged_DoesNotCopyFile()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\test.txt",
            DestinationRoot = @"D:\backup"
        };
        
        // Mock same hashes for source and destination
        var destPath = Path.Combine(rule.DestinationRoot, Path.GetFileName(rule.SourceFile));
        _mockFileSystem.Setup(x => x.FileExists(destPath)).Returns(true);
        _mockHashService.Setup(x => x.ComputeHash(destPath)).Returns("same_hash");
        _mockFileSystem.Setup(x => x.FileExists(rule.SourceFile)).Returns(true);
        _mockHashService.Setup(x => x.ComputeHash(rule.SourceFile)).Returns("same_hash");

        // Act
        await _pollerService.ProcessRuleAsync(rule);

        // Assert
        _mockHashService.Verify(x => x.ComputeHash(rule.SourceFile), Times.Once);
        _mockCopyService.Verify(x => x.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ProcessRuleAsync_FileChanged_CopiesFileAndExecutesPostAction()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\Work\test.txt",
            DestinationRoot = @"D:\Backup",
            PostAction = PostActionType.None
        };
        
        // Mock different hashes for source and destination
        var destPath = Path.Combine(rule.DestinationRoot, Path.GetFileName(rule.SourceFile));
        _mockFileSystem.Setup(x => x.FileExists(destPath)).Returns(true);
        _mockHashService.Setup(x => x.ComputeHash(destPath)).Returns("old_hash");
        var newHash = "new_hash";
        var eventTriggered = false;
        
        _mockFileSystem.Setup(x => x.FileExists(rule.SourceFile)).Returns(true);
        _mockHashService.Setup(x => x.ComputeHash(rule.SourceFile)).Returns(newHash);
        _mockCopyService.Setup(x => x.CopyFileAsync(rule.SourceFile, rule.DestinationRoot, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockPathSanitizer.Setup(x => x.SanitizePath(@"C:\Work")).Returns(@"C__Work");
        _mockPostCopyService.Setup(x => x.ExecutePostActionAsync(
            PostActionType.None, 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _pollerService.FileChanged += (sender, fileName) => { eventTriggered = true; };

        // Act
        await _pollerService.ProcessRuleAsync(rule);

        // Assert
        Assert.That(eventTriggered, Is.True);
        _mockLogService.Verify(x => x.Info(It.Is<string>(s => s.Contains("File changed detected")), nameof(PollerService)), Times.Once);
        _mockCopyService.Verify(x => x.CopyFileAsync(rule.SourceFile, rule.DestinationRoot, It.IsAny<CancellationToken>()), Times.Once);
        _mockPostCopyService.Verify(x => x.ExecutePostActionAsync(
            PostActionType.None, 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProcessRuleAsync_CopyFails_DoesNotExecutePostAction()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\test.txt",
            DestinationRoot = @"D:\Backup"
        };
        
        // Mock different hashes for source and destination
        var destPath = Path.Combine(rule.DestinationRoot, Path.GetFileName(rule.SourceFile));
        _mockFileSystem.Setup(x => x.FileExists(destPath)).Returns(true);
        _mockHashService.Setup(x => x.ComputeHash(destPath)).Returns("old_hash");
        var newHash = "new_hash";
        
        _mockFileSystem.Setup(x => x.FileExists(rule.SourceFile)).Returns(true);
        _mockHashService.Setup(x => x.ComputeHash(rule.SourceFile)).Returns(newHash);
        _mockCopyService.Setup(x => x.CopyFileAsync(rule.SourceFile, rule.DestinationRoot, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _pollerService.ProcessRuleAsync(rule);

        // Assert
        _mockCopyService.Verify(x => x.CopyFileAsync(rule.SourceFile, rule.DestinationRoot, It.IsAny<CancellationToken>()), Times.Once);
        _mockPostCopyService.Verify(x => x.ExecutePostActionAsync(
            It.IsAny<PostActionType>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ProcessRuleAsync_ThrowsException_LogsError()
    {
        // Arrange
        var rule = new WatchRule 
        { 
            SourceFile = @"C:\test.txt" 
        };
        var exceptionMessage = "Test exception";
        
        _mockFileSystem.Setup(x => x.FileExists(rule.SourceFile)).Throws(new Exception(exceptionMessage));

        // Act
        await _pollerService.ProcessRuleAsync(rule);

        // Assert
        _mockLogService.Verify(x => x.Error(
            It.Is<string>(s => s.Contains(exceptionMessage) && s.Contains(rule.SourceFile)), 
            nameof(PollerService)), Times.Once);
    }
}