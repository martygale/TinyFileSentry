using Moq;
using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Models;
using TinyFileSentry.Core.Services;

namespace TinyFileSentry.Tests.Services;

[TestFixture]
public class RulesServiceTests
{
    private Mock<IConfigurationService> _configurationServiceMock = null!;
    private Mock<ILogService> _logServiceMock = null!;
    private RulesService _rulesService = null!;
    private Configuration _testConfiguration = null!;

    [SetUp]
    public void Setup()
    {
        _configurationServiceMock = new Mock<IConfigurationService>();
        _logServiceMock = new Mock<ILogService>();
        
        _testConfiguration = new Configuration
        {
            PollingSpeed = PollingSpeed.Fast,
            WatchRules = new List<WatchRule>()
        };

        _configurationServiceMock.Setup(x => x.LoadConfiguration()).Returns(_testConfiguration);
        _rulesService = new RulesService(_configurationServiceMock.Object, _logServiceMock.Object);
    }

    [Test]
    public void GetWatchRules_ReturnsAllRules()
    {
        var rule1 = new WatchRule { SourceFile = "file1.txt", DestinationRoot = "/dest1" };
        var rule2 = new WatchRule { SourceFile = "file2.txt", DestinationRoot = "/dest2" };
        _testConfiguration.WatchRules.AddRange(new[] { rule1, rule2 });

        var result = _rulesService.GetWatchRules().ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].SourceFile, Is.EqualTo("file1.txt"));
        Assert.That(result[1].SourceFile, Is.EqualTo("file2.txt"));
    }

    [Test]
    public void AddWatchRule_AddsRuleAndSavesConfiguration()
    {
        var rule = new WatchRule { SourceFile = "test.txt", DestinationRoot = "/backup" };

        _rulesService.AddWatchRule(rule);

        Assert.That(_testConfiguration.WatchRules, Has.Count.EqualTo(1));
        Assert.That(_testConfiguration.WatchRules[0], Is.EqualTo(rule));
        _configurationServiceMock.Verify(x => x.SaveConfiguration(_testConfiguration), Times.Once);
        _logServiceMock.Verify(x => x.Info(It.Is<string>(s => s.Contains("Watch rule added")), nameof(RulesService)), Times.Once);
    }

    [Test]
    public void AddWatchRule_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _rulesService.AddWatchRule(null!));
    }

    [Test]
    public void RemoveWatchRule_RemovesExistingRule()
    {
        var rule = new WatchRule { SourceFile = "test.txt", DestinationRoot = "/backup" };
        _testConfiguration.WatchRules.Add(rule);

        _rulesService.RemoveWatchRule(rule);

        Assert.That(_testConfiguration.WatchRules, Is.Empty);
        _configurationServiceMock.Verify(x => x.SaveConfiguration(_testConfiguration), Times.Once);
        _logServiceMock.Verify(x => x.Info(It.Is<string>(s => s.Contains("Watch rule removed")), nameof(RulesService)), Times.Once);
    }

    [Test]
    public void RemoveWatchRule_WithNonExistentRule_DoesNothing()
    {
        var rule = new WatchRule { SourceFile = "nonexistent.txt", DestinationRoot = "/backup" };

        _rulesService.RemoveWatchRule(rule);

        _configurationServiceMock.Verify(x => x.SaveConfiguration(It.IsAny<Configuration>()), Times.Never);
        _logServiceMock.Verify(x => x.Warning(It.Is<string>(s => s.Contains("not found for removal")), nameof(RulesService)), Times.Once);
    }

    [Test]
    public void UpdateWatchRule_UpdatesExistingRule()
    {
        var originalRule = new WatchRule { SourceFile = "test.txt", DestinationRoot = "/backup" };
        _testConfiguration.WatchRules.Add(originalRule);

        var updatedRule = new WatchRule 
        { 
            SourceFile = "test.txt", 
            DestinationRoot = "/newbackup",
            PostAction = PostActionType.GitCommit
        };

        _rulesService.UpdateWatchRule(updatedRule);

        Assert.That(originalRule.DestinationRoot, Is.EqualTo("/newbackup"));
        Assert.That(originalRule.PostAction, Is.EqualTo(PostActionType.GitCommit));
        _configurationServiceMock.Verify(x => x.SaveConfiguration(_testConfiguration), Times.Once);
        _logServiceMock.Verify(x => x.Info(It.Is<string>(s => s.Contains("Watch rule updated")), nameof(RulesService)), Times.Once);
    }

    // UpdateRuleHash method was removed as LastHash property no longer exists
    // [Test]
    // public void UpdateRuleHash_UpdatesHashAndSaves() - тест удален


    [Test]
    public void RuleAdded_EventIsFired()
    {
        WatchRule? capturedRule = null;
        _rulesService.RuleAdded += (sender, rule) => capturedRule = rule;

        var rule = new WatchRule { SourceFile = "test.txt", DestinationRoot = "/backup" };
        _rulesService.AddWatchRule(rule);

        Assert.That(capturedRule, Is.Not.Null);
        Assert.That(capturedRule.SourceFile, Is.EqualTo("test.txt"));
    }
}