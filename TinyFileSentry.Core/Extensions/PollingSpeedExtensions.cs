using TinyFileSentry.Core.Models;

namespace TinyFileSentry.Core.Extensions;

/// <summary>
/// Extension methods for PollingSpeed
/// </summary>
public static class PollingSpeedExtensions
{
    /// <summary>
    /// Converts PollingSpeed value to seconds
    /// </summary>
    /// <param name="pollingSpeed">Polling speed</param>
    /// <returns>Interval in seconds</returns>
    public static int ToSeconds(this PollingSpeed pollingSpeed)
    {
        return (int)pollingSpeed;
    }

    /// <summary>
    /// Converts ComboBox index to PollingSpeed
    /// </summary>
    /// <param name="index">Index: 0=Slow, 1=Medium, 2=Fast</param>
    /// <returns>PollingSpeed value</returns>
    public static PollingSpeed FromComboBoxIndex(int index)
    {
        return index switch
        {
            0 => PollingSpeed.Slow,
            1 => PollingSpeed.Medium,
            2 => PollingSpeed.Fast,
            _ => PollingSpeed.Fast // Default to fast speed
        };
    }

    /// <summary>
    /// Converts PollingSpeed to ComboBox index
    /// </summary>
    /// <param name="pollingSpeed">Polling speed</param>
    /// <returns>Index: 0=Slow, 1=Medium, 2=Fast</returns>
    public static int ToComboBoxIndex(this PollingSpeed pollingSpeed)
    {
        return pollingSpeed switch
        {
            PollingSpeed.Slow => 0,
            PollingSpeed.Medium => 1,
            PollingSpeed.Fast => 2,
            _ => 2 // Default to fast speed (index 2)
        };
    }
}