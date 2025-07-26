using Moq;
using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Services;

namespace TinyFileSentry.Tests.Services;

[TestFixture]
public class HashServiceTests
{
    private Mock<IFileSystem> _fileSystemMock = null!;
    private HashService _hashService = null!;

    [SetUp]
    public void Setup()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _hashService = new HashService(_fileSystemMock.Object);
    }

    [Test]
    public void ComputeHash_WithByteArray_ReturnsCorrectHash()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var result = _hashService.ComputeHash(data);
        
        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Length, Is.EqualTo(64)); // SHA256 hash in hex format
    }

    [Test]
    public void ComputeHash_WithSameData_ReturnsSameHash()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        
        var hash1 = _hashService.ComputeHash(data);
        var hash2 = _hashService.ComputeHash(data);
        
        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void ComputeHash_WithDifferentData_ReturnsDifferentHash()
    {
        var data1 = new byte[] { 1, 2, 3, 4, 5 };
        var data2 = new byte[] { 1, 2, 3, 4, 6 };
        
        var hash1 = _hashService.ComputeHash(data1);
        var hash2 = _hashService.ComputeHash(data2);
        
        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void ComputeHash_WithFilePath_ReturnsCorrectHash()
    {
        var filePath = "test.txt";
        var fileData = new byte[] { 1, 2, 3, 4, 5 };
        _fileSystemMock.Setup(x => x.ReadAllBytes(filePath)).Returns(fileData);

        var result = _hashService.ComputeHash(filePath);
        var expectedHash = _hashService.ComputeHash(fileData);

        Assert.That(result, Is.EqualTo(expectedHash));
        _fileSystemMock.Verify(x => x.ReadAllBytes(filePath), Times.Once);
    }

    [Test]
    public void ComputeHash_WithEmptyData_ReturnsValidHash()
    {
        var data = Array.Empty<byte>();
        var result = _hashService.ComputeHash(data);
        
        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Length, Is.EqualTo(64));
    }
}