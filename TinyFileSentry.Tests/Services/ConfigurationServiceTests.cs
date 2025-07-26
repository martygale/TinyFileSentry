using Moq;
using TinyFileSentry.Core.Extensions;
using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Models;
using TinyFileSentry.Core.Services;

namespace TinyFileSentry.Tests.Services;

[TestFixture]
public class ConfigurationServiceTests
{
    private Mock<IFileSystem> _fileSystemMock = null!;
    private Mock<ILogService> _logServiceMock = null!;
    private ConfigurationService _configurationService = null!;

    [SetUp]
    public void Setup()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _logServiceMock = new Mock<ILogService>();
        _configurationService = new ConfigurationService(_fileSystemMock.Object, _logServiceMock.Object);
    }

    [Test]
    public void LoadConfiguration_WhenFileDoesNotExist_CreatesDefaultConfiguration()
    {
        _fileSystemMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);

        var result = _configurationService.LoadConfiguration();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.PollingSpeed, Is.EqualTo(PollingSpeed.Fast));
        Assert.That(result.IsMonitoringActive, Is.True);
        Assert.That(result.WatchRules, Is.Empty);
        _logServiceMock.Verify(x => x.Info(It.Is<string>(s => s.Contains("creating default")), nameof(ConfigurationService)), Times.Once);
    }

    [Test]
    public void GetConfigurationPath_ReturnsValidPath()
    {
        var path = _configurationService.GetConfigurationPath();
        
        Assert.That(path, Is.Not.Null.And.Not.Empty);
        Assert.That(path, Does.EndWith("config.json"));
    }

    [Test]
    public void SaveConfiguration_CreatesDirectoryIfNeeded()
    {
        var config = new Configuration { PollingSpeed = PollingSpeed.Medium };
        var configPath = _configurationService.GetConfigurationPath();
        var directory = Path.GetDirectoryName(configPath);

        _fileSystemMock.Setup(x => x.DirectoryExists(directory!)).Returns(false);

        _configurationService.SaveConfiguration(config);

        _fileSystemMock.Verify(x => x.CreateDirectory(directory!), Times.Once);
        _logServiceMock.Verify(x => x.Info("Configuration saved successfully", nameof(ConfigurationService)), Times.Once);
    }

    [Test]
    public void SaveConfiguration_WithExistingDirectory_DoesNotCreateDirectory()
    {
        var config = new Configuration { PollingSpeed = PollingSpeed.Medium };
        var configPath = _configurationService.GetConfigurationPath();
        var directory = Path.GetDirectoryName(configPath);

        _fileSystemMock.Setup(x => x.DirectoryExists(directory!)).Returns(true);

        _configurationService.SaveConfiguration(config);

        _fileSystemMock.Verify(x => x.CreateDirectory(It.IsAny<string>()), Times.Never);
        _logServiceMock.Verify(x => x.Info("Configuration saved successfully", nameof(ConfigurationService)), Times.Once);
    }

    [Test]
    public void LoadConfiguration_LoadsPollingSpeedCorrectly()
    {
        // Create configuration with PollingSpeed.Medium (60 seconds)
        var config = new Configuration { PollingSpeed = PollingSpeed.Medium };
        var json = System.Text.Json.JsonSerializer.Serialize(config);
        
        _fileSystemMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(json);

        var result = _configurationService.LoadConfiguration();

        // Verify that PollingSpeed loads correctly
        Assert.That(result.PollingSpeed, Is.EqualTo(PollingSpeed.Medium));
    }

    [Test]
    public void LoadConfiguration_PreservesIsMonitoringActiveState()
    {
        // Create configuration with IsMonitoringActive = false
        var config = new Configuration { IsMonitoringActive = false, PollingSpeed = PollingSpeed.Fast };
        var json = System.Text.Json.JsonSerializer.Serialize(config);
        
        _fileSystemMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(json);

        var result = _configurationService.LoadConfiguration();

        Assert.That(result.IsMonitoringActive, Is.False);
        Assert.That(result.PollingSpeed, Is.EqualTo(PollingSpeed.Fast));
    }

    [Test]
    public void LoadConfiguration_WithInvalidPollingSpeedInJson_SetsToFast()
    {
        // Create JSON with invalid PollingSpeed value
        var invalidJson = """
        {
            "pollingSpeed": "Invalid",
            "pollingIntervalSec": 5,
            "watchRules": []
        }
        """;
        
        _fileSystemMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(invalidJson);

        var result = _configurationService.LoadConfiguration();

        // Invalid JSON should default to Fast value
        Assert.That(result.PollingSpeed, Is.EqualTo(PollingSpeed.Fast));
    }

    [Test]
    [TestCase(PollingSpeed.Slow, 600)]
    [TestCase(PollingSpeed.Medium, 60)]
    [TestCase(PollingSpeed.Fast, 10)]
    public void PollingSpeed_ToSeconds_ReturnsCorrectValues(PollingSpeed speed, int expectedSeconds)
    {
        var result = speed.ToSeconds();
        Assert.That(result, Is.EqualTo(expectedSeconds));
    }

    [Test]
    [TestCase(0, PollingSpeed.Slow)]
    [TestCase(1, PollingSpeed.Medium)]
    [TestCase(2, PollingSpeed.Fast)]
    [TestCase(99, PollingSpeed.Fast)] // Invalid index should return Fast
    public void PollingSpeedExtensions_FromComboBoxIndex_ReturnsCorrectSpeed(int index, PollingSpeed expectedSpeed)
    {
        var result = PollingSpeedExtensions.FromComboBoxIndex(index);
        Assert.That(result, Is.EqualTo(expectedSpeed));
    }

    [Test]
    [TestCase(PollingSpeed.Slow, 0)]
    [TestCase(PollingSpeed.Medium, 1)]
    [TestCase(PollingSpeed.Fast, 2)]
    public void PollingSpeedExtensions_ToComboBoxIndex_ReturnsCorrectIndex(PollingSpeed speed, int expectedIndex)
    {
        var result = speed.ToComboBoxIndex();
        Assert.That(result, Is.EqualTo(expectedIndex));
    }
}