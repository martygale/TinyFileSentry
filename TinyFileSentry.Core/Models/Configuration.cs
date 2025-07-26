using System.Text.Json.Serialization;

namespace TinyFileSentry.Core.Models;

public class Configuration
{
    [JsonPropertyName("pollingSpeed")]
    public PollingSpeed PollingSpeed { get; set; } = PollingSpeed.Fast;

    [JsonPropertyName("isMonitoringActive")]
    public bool IsMonitoringActive { get; set; } = true;

    [JsonPropertyName("autoStart")]
    public bool AutoStart { get; set; } = false;

    [JsonPropertyName("watchRules")]
    public List<WatchRule> WatchRules { get; set; } = new();
}