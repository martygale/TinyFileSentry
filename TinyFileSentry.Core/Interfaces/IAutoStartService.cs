namespace TinyFileSentry.Core.Interfaces;

/// <summary>
/// Interface for managing application auto-start with Windows
/// </summary>
public interface IAutoStartService
{
    /// <summary>
    /// Enables application auto-start with Windows
    /// </summary>
    /// <param name="applicationPath">Path to application executable file</param>
    /// <returns>True if auto-start successfully enabled</returns>
    bool EnableAutoStart(string applicationPath);

    /// <summary>
    /// Disables application auto-start with Windows
    /// </summary>
    /// <returns>True if auto-start successfully disabled</returns>
    bool DisableAutoStart();

    /// <summary>
    /// Checks if application auto-start is enabled
    /// </summary>
    /// <returns>True if auto-start is enabled</returns>
    bool IsAutoStartEnabled();

    /// <summary>
    /// Gets current path to executable file in auto-start
    /// </summary>
    /// <returns>Path to file or null if auto-start is disabled</returns>
    string? GetAutoStartPath();
}