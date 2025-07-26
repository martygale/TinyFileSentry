using Moq;
using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Services;

namespace TinyFileSentry.Tests.Services;

[TestFixture]
public class CopyServiceTests
{
    private Mock<IFileSystem> _fileSystemMock = null!;
    private Mock<ILogService> _logServiceMock = null!;
    private Mock<IPathSanitizer> _pathSanitizerMock = null!;
    private CopyService _copyService = null!;

    [SetUp]
    public void Setup()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _logServiceMock = new Mock<ILogService>();
        _pathSanitizerMock = new Mock<IPathSanitizer>();
        _copyService = new CopyService(_fileSystemMock.Object, _logServiceMock.Object, _pathSanitizerMock.Object);
    }

    [Test]
    public async Task CopyFileAsync_WithValidFile_ReturnsTrue()
    {
        var sourcePath = @"C:\source\test.txt";
        var destinationRoot = @"D:\backup";
        var fileSize = 1024;

        _fileSystemMock.Setup(x => x.FileExists(sourcePath)).Returns(true);
        _fileSystemMock.Setup(x => x.GetFileSize(sourcePath)).Returns(fileSize);
        _pathSanitizerMock.Setup(x => x.SanitizePath(It.IsAny<string>())).Returns("c_source");
        _fileSystemMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);
        _fileSystemMock.Setup(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), true));

        var result = await _copyService.CopyFileAsync(sourcePath, destinationRoot);

        
        Assert.That(result, Is.True);
        _fileSystemMock.Verify(x => x.CreateDirectory(It.IsAny<string>()), Times.Once);
        _fileSystemMock.Verify(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), true), Times.Once);
        _logServiceMock.Verify(x => x.Info(It.IsAny<string>(), nameof(CopyService)), Times.Once);
    }

    [Test]
    public async Task CopyFileAsync_WithNonExistentFile_ReturnsFalse()
    {
        var sourcePath = @"C:\source\nonexistent.txt";
        var destinationRoot = @"D:\backup";

        _fileSystemMock.Setup(x => x.FileExists(sourcePath)).Returns(false);

        var result = await _copyService.CopyFileAsync(sourcePath, destinationRoot);

        Assert.That(result, Is.False);
        _logServiceMock.Verify(x => x.Warning(It.IsAny<string>(), nameof(CopyService)), Times.Once);
    }

    [Test]
    public async Task CopyFileAsync_WithLargeFile_ReturnsFalse()
    {
        var sourcePath = @"C:\source\largefile.txt";
        var destinationRoot = @"D:\backup";
        var fileSize = 11 * 1024 * 1024; // 11 MB

        _fileSystemMock.Setup(x => x.FileExists(sourcePath)).Returns(true);
        _fileSystemMock.Setup(x => x.GetFileSize(sourcePath)).Returns(fileSize);

        var result = await _copyService.CopyFileAsync(sourcePath, destinationRoot);

        Assert.That(result, Is.False);
        _logServiceMock.Verify(x => x.Warning(It.Is<string>(s => s.Contains("exceeds limit")), nameof(CopyService)), Times.Once);
    }

    [Test]
    public async Task CopyFileAsync_WithIOException_RetriesAndFails()
    {
        var sourcePath = @"C:\source\test.txt";
        var destinationRoot = @"D:\backup";

        _fileSystemMock.Setup(x => x.FileExists(sourcePath)).Returns(true);
        _fileSystemMock.Setup(x => x.GetFileSize(sourcePath)).Returns(1024);
        _pathSanitizerMock.Setup(x => x.SanitizePath(It.IsAny<string>())).Returns("c_source");
        _fileSystemMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Throws(new IOException("File is locked"));

        var result = await _copyService.CopyFileAsync(sourcePath, destinationRoot);

        Assert.That(result, Is.False);
        _fileSystemMock.Verify(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), true), Times.Exactly(10));
        _logServiceMock.Verify(x => x.Error(It.IsAny<string>(), nameof(CopyService)), Times.Once);
    }

    [Test]
    public async Task CopyFileAsync_WithIOExceptionThenSuccess_ReturnsTrue()
    {
        var sourcePath = @"C:\source\test.txt";
        var destinationRoot = @"D:\backup";

        _fileSystemMock.Setup(x => x.FileExists(sourcePath)).Returns(true);
        _fileSystemMock.Setup(x => x.GetFileSize(sourcePath)).Returns(1024);
        _pathSanitizerMock.Setup(x => x.SanitizePath(It.IsAny<string>())).Returns("c_source");
        _fileSystemMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
        
        var callCount = 0;
        _fileSystemMock.Setup(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Callback(() => 
            {
                callCount++;
                if (callCount <= 2)
                    throw new IOException("File is locked");
            });

        var result = await _copyService.CopyFileAsync(sourcePath, destinationRoot);

        Assert.That(result, Is.True);
        _fileSystemMock.Verify(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), true), Times.Exactly(3));
        _logServiceMock.Verify(x => x.Warning(It.Is<string>(s => s.Contains("Copy attempt")), nameof(CopyService)), Times.Exactly(2));
        _logServiceMock.Verify(x => x.Info(It.Is<string>(s => s.Contains("copied successfully")), nameof(CopyService)), Times.Once);
    }
}