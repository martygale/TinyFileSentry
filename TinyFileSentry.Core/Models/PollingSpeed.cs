using System.Text.Json.Serialization;

namespace TinyFileSentry.Core.Models;

/// <summary>
/// Defines file polling speed
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PollingSpeed
{
    /// <summary>
    /// Slow speed - 600 seconds (10 minutes)
    /// </summary>
    Slow = 600,

    /// <summary>
    /// Medium speed - 60 seconds (1 minute)
    /// </summary>
    Medium = 60,

    /// <summary>
    /// Fast speed - 10 seconds
    /// </summary>
    Fast = 10
}