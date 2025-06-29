using Moq;
using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Services;

namespace TinyFileSentry.Tests.Services;

[TestFixture]
public class PathSanitizerTests
{
    private Mock<IFileSystem> _fileSystemMock = null!;
    private PathSanitizer _pathSanitizer = null!;

    [SetUp]
    public void Setup()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _fileSystemMock.Setup(x => x.GetInvalidFileNameChars())
            .Returns(new[] { "<", ">", ":", "\"", "|", "?", "*" });
        _pathSanitizer = new PathSanitizer(_fileSystemMock.Object);
    }

    [Test]
    public void SanitizePath_WithWindowsPath_ReplacesColonAndBackslashes()
    {
        var path = @"C:\Work\Documents";
        var result = _pathSanitizer.SanitizePath(path);
        
        Assert.That(result, Is.EqualTo("c_work_documents"));
    }

    [Test]
    public void SanitizePath_WithInvalidChars_ReplacesWithUnderscore()
    {
        var path = @"Test<File>Name:With|Invalid*Chars";
        var result = _pathSanitizer.SanitizePath(path);
        
        Assert.That(result, Is.EqualTo("test_file_name_with_invalid_chars"));
    }

    [Test]
    public void SanitizePath_WithEmptyString_ReturnsEmpty()
    {
        var result = _pathSanitizer.SanitizePath("");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SanitizePath_WithNull_ReturnsEmpty()
    {
        var result = _pathSanitizer.SanitizePath(null!);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SanitizePath_WithValidPath_ReturnsLowerCase()
    {
        var path = "ValidPathName123";
        var result = _pathSanitizer.SanitizePath(path);
        
        Assert.That(result, Is.EqualTo("validpathname123"));
    }

    [Test]
    public void SanitizePath_ComplexExample_SanitizesCorrectly()
    {
        var path = @"C:\Work\common\report.docx";
        var result = _pathSanitizer.SanitizePath(path);
        
        Assert.That(result, Is.EqualTo("c_work_common_report.docx"));
    }

    [Test]
    public void SanitizePath_WithMixedCase_ConvertsToLowerCase()
    {
        var path = "MyProject_BACKUP_Folder";
        var result = _pathSanitizer.SanitizePath(path);
        
        Assert.That(result, Is.EqualTo("myproject_backup_folder"));
    }

    [Test]
    public void SanitizePath_WithUppercaseWindowsPath_ConvertsToLowerCase()
    {
        var path = @"D:\DEVELOPMENT\PROJECT";
        var result = _pathSanitizer.SanitizePath(path);
        
        Assert.That(result, Is.EqualTo("d_development_project"));
    }

    [Test]
    public void SanitizePath_WithLongPath_TruncatesTo200Characters()
    {
        // Создаем путь длиннее 200 символов
        var longPath = new string('a', 250); // 250 символов 'a'
        var result = _pathSanitizer.SanitizePath(longPath);
        
        Assert.That(result.Length, Is.EqualTo(200));
        Assert.That(result, Is.EqualTo(new string('a', 200)));
    }

    [Test]
    public void SanitizePath_WithExactly200Characters_ReturnsUnchanged()
    {
        // Создаем путь ровно в 200 символов
        var path200 = new string('b', 200);
        var result = _pathSanitizer.SanitizePath(path200);
        
        Assert.That(result.Length, Is.EqualTo(200));
        Assert.That(result, Is.EqualTo(path200));
    }

    [Test]
    public void SanitizePath_WithLongPathAndInvalidChars_TruncatesAfterSanitization()
    {
        // Длинный путь с недопустимыми символами (более 200 символов)
        var longPathWithInvalidChars = @"C:\Very\Long\Path\With\Many\Subdirectories\And\Some\Invalid<Chars>That\Need\To\Be\Replaced\With\Underscores\But\Also\Needs\To\Be\Truncated\Because\It\Is\Too\Long\For\Our\Limit\And\Even\More\Extra\Text\To\Make\Sure\We\Exceed\Two\Hundred\Characters\In\Total\Length";
        var result = _pathSanitizer.SanitizePath(longPathWithInvalidChars);
        
        Assert.That(result.Length, Is.EqualTo(200));
        // Проверяем, что недопустимые символы заменены на подчеркивания
        Assert.That(result, Does.Not.Contain("<"));
        Assert.That(result, Does.Not.Contain(">"));
        // Проверяем, что путь в нижнем регистре
        Assert.That(result, Is.EqualTo(result.ToLowerInvariant()));
    }
}