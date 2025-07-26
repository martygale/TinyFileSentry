using Moq;
using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Models;
using TinyFileSentry.Core.Services;

namespace TinyFileSentry.Tests.Services;

[TestFixture]
public class LogServiceTests
{
    private Mock<IClock> _clockMock = null!;
    private LogService _logService = null!;
    private DateTime _testDateTime = new(2025, 6, 22, 14, 5, 2, DateTimeKind.Utc);

    [SetUp]
    public void Setup()
    {
        _clockMock = new Mock<IClock>();
        _clockMock.Setup(x => x.UtcNow).Returns(_testDateTime);
        _logService = new LogService(_clockMock.Object);
    }

    [Test]
    public void Log_AddsEntryToLogs()
    {
        var message = "Test message";
        var source = "TestSource";

        _logService.Log(LogLevel.Info, message, source);

        var logs = _logService.GetLogs().ToList();
        Assert.That(logs, Has.Count.EqualTo(1));
        Assert.That(logs[0].Message, Is.EqualTo(message));
        Assert.That(logs[0].Source, Is.EqualTo(source));
        Assert.That(logs[0].Level, Is.EqualTo(LogLevel.Info));
        Assert.That(logs[0].Timestamp, Is.EqualTo(_testDateTime));
    }

    [Test]
    public void Info_AddsInfoLevelEntry()
    {
        var message = "Info message";
        
        _logService.Info(message);

        var logs = _logService.GetLogs().ToList();
        Assert.That(logs[0].Level, Is.EqualTo(LogLevel.Info));
        Assert.That(logs[0].Message, Is.EqualTo(message));
    }

    [Test]
    public void Warning_AddsWarningLevelEntry()
    {
        var message = "Warning message";
        
        _logService.Warning(message);

        var logs = _logService.GetLogs().ToList();
        Assert.That(logs[0].Level, Is.EqualTo(LogLevel.Warning));
        Assert.That(logs[0].Message, Is.EqualTo(message));
    }

    [Test]
    public void Error_AddsErrorLevelEntry()
    {
        var message = "Error message";
        
        _logService.Error(message);

        var logs = _logService.GetLogs().ToList();
        Assert.That(logs[0].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(logs[0].Message, Is.EqualTo(message));
    }

    [Test]
    public void LogAdded_EventIsFired()
    {
        LogEntry? capturedLogEntry = null;
        _logService.LogAdded += (sender, logEntry) => capturedLogEntry = logEntry;

        var message = "Test message";
        _logService.Info(message);

        Assert.That(capturedLogEntry, Is.Not.Null);
        Assert.That(capturedLogEntry.Message, Is.EqualTo(message));
        Assert.That(capturedLogEntry.Level, Is.EqualTo(LogLevel.Info));
    }

    [Test]
    public void LogService_MaintainsMaximumLogEntries()
    {
        for (int i = 0; i < 10005; i++)
        {
            _logService.Info($"Message {i}");
        }

        var logs = _logService.GetLogs().ToList();
        Assert.That(logs, Has.Count.EqualTo(10000));
        
        Assert.That(logs.First().Message, Is.EqualTo("Message 5"));
        Assert.That(logs.Last().Message, Is.EqualTo("Message 10004"));
    }

    [Test]
    public void GetLogs_ReturnsLogsInCorrectOrder()
    {
        _logService.Info("First message");
        _logService.Info("Second message");
        _logService.Info("Third message");

        var logs = _logService.GetLogs().ToList();
        Assert.That(logs, Has.Count.EqualTo(3));
        Assert.That(logs[0].Message, Is.EqualTo("First message"));
        Assert.That(logs[1].Message, Is.EqualTo("Second message"));
        Assert.That(logs[2].Message, Is.EqualTo("Third message"));
    }
}