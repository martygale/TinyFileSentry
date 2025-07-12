using System.Text.Json.Serialization;

namespace TinyFileSentry.Core.Models;

public class WatchRule
{
    [JsonPropertyName("sourceFile")]
    public string SourceFile { get; set; } = string.Empty;

    [JsonPropertyName("destinationRoot")]
    public string DestinationRoot { get; set; } = string.Empty;

    [JsonPropertyName("postAction")]
    public PostActionType PostAction { get; set; } = PostActionType.None;


    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [JsonIgnore]
    public RuleStatus Status { get; set; } = RuleStatus.Synchronized;
}