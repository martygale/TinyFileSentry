using TinyFileSentry.Core.Adapters;
using TinyFileSentry.Core.Extensions;
using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Models;
using TinyFileSentry.Core.Services;

namespace TinyFileSentry.Tests.Integration;

[TestFixture]
public class IntegrationTests
{
    private string _tempDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Test]
    public void ConfigurationService_LoadAndSave_WorksCorrectly()
    {
        var fileSystem = new FileSystemAdapter();
        var clock = new SystemClock();
        var logService = new LogService(clock);
        
        var tempConfigPath = Path.Combine(_tempDirectory, "config.json");
        var configService = new TestableConfigurationService(fileSystem, logService, tempConfigPath);

        var config = new Configuration
        {
            PollingSpeed = PollingSpeed.Medium, // 60 seconds
            WatchRules = new List<WatchRule>
            {
                new() { SourceFile = "test.txt", DestinationRoot = "/backup", PostAction = PostActionType.GitCommit }
            }
        };

        configService.SaveConfiguration(config);
        Assert.That(File.Exists(tempConfigPath), Is.True);

        var loadedConfig = configService.LoadConfiguration();
        Assert.That(loadedConfig.PollingSpeed.ToSeconds(), Is.EqualTo(60)); // Verify that PollingSpeed.Medium = 60 seconds
        Assert.That(loadedConfig.PollingSpeed, Is.EqualTo(PollingSpeed.Medium));
        Assert.That(loadedConfig.WatchRules, Has.Count.EqualTo(1));
        Assert.That(loadedConfig.WatchRules[0].SourceFile, Is.EqualTo("test.txt"));
        Assert.That(loadedConfig.WatchRules[0].PostAction, Is.EqualTo(PostActionType.GitCommit));
    }

    [Test]
    public async Task CopyService_Integration_WorksCorrectly()
    {
        var fileSystem = new FileSystemAdapter();
        var clock = new SystemClock();
        var logService = new LogService(clock);
        var pathSanitizer = new PathSanitizer(fileSystem);
        var copyService = new CopyService(fileSystem, logService, pathSanitizer);

        var sourceFile = Path.Combine(_tempDirectory, "source.txt");
        var destinationRoot = Path.Combine(_tempDirectory, "backup");
        
        File.WriteAllText(sourceFile, "Test content");

        var result = await copyService.CopyFileAsync(sourceFile, destinationRoot);

        Assert.That(result, Is.True);
        
        // Use PathSanitizer to get the correct destination path
        var sourceDirectory = Path.GetDirectoryName(sourceFile) ?? string.Empty;
        var sanitizedPath = pathSanitizer.SanitizePath(sourceDirectory);
        var expectedDestPath = Path.Combine(destinationRoot, sanitizedPath, "source.txt");
        Assert.That(File.Exists(expectedDestPath), Is.True);
        Assert.That(File.ReadAllText(expectedDestPath), Is.EqualTo("Test content"));
    }

    [Test]
    public void HashService_Integration_WorksCorrectly()
    {
        var fileSystem = new FileSystemAdapter();
        var hashService = new HashService(fileSystem);

        var testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Hello World");

        var hash1 = hashService.ComputeHash(testFile);
        var hash2 = hashService.ComputeHash(testFile);

        Assert.That(hash1, Is.EqualTo(hash2));
        Assert.That(hash1, Has.Length.EqualTo(64)); // SHA256 hex length

        File.WriteAllText(testFile, "Hello World!");
        var hash3 = hashService.ComputeHash(testFile);

        Assert.That(hash3, Is.Not.EqualTo(hash1));
    }

    private class TestableConfigurationService : ConfigurationService
    {
        private readonly string _configPath;

        public TestableConfigurationService(IFileSystem fileSystem, ILogService logService, string configPath) 
            : base(fileSystem, logService)
        {
            _configPath = configPath;
        }

        public override string GetConfigurationPath() => _configPath;
    }
}