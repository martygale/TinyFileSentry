using System.Text.Json.Serialization;

namespace TinyFileSentry.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PostActionType
{
    None,
    GitCommit,
    GitCommitAndPush
}