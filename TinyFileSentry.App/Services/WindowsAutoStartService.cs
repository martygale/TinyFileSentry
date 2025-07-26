using System;
using Microsoft.Win32;
using TinyFileSentry.Core.Interfaces;

namespace TinyFileSentry.App.Services;

/// <summary>
/// Windows implementation of auto-start service through registry
/// </summary>
public class WindowsAutoStartService : IAutoStartService
{
    private const string REGISTRY_KEY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string APPLICATION_NAME = "TinyFileSentry";

    /// <summary>
    /// Enables application auto-start with Windows
    /// </summary>
    public bool EnableAutoStart(string applicationPath)
    {
        if (string.IsNullOrWhiteSpace(applicationPath))
        {
            throw new ArgumentException("Application path cannot be empty", nameof(applicationPath));
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, writable: true);
            if (key == null)
            {
                // Key doesn't exist, create it
                using var newKey = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY_PATH);
                newKey?.SetValue(APPLICATION_NAME, applicationPath);
            }
            else
            {
                key.SetValue(APPLICATION_NAME, applicationPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            // Log error but don't interrupt application
            System.Diagnostics.Debug.WriteLine($"Failed to enable auto-start: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Disables application auto-start with Windows
    /// </summary>
    public bool DisableAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, writable: true);
            if (key != null)
            {
                key.DeleteValue(APPLICATION_NAME, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex)
        {
            // Log error but don't interrupt application
            System.Diagnostics.Debug.WriteLine($"Failed to disable auto-start: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if application auto-start is enabled
    /// </summary>
    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, writable: false);
            var value = key?.GetValue(APPLICATION_NAME) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to check auto-start status: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets current path to executable file in auto-start
    /// </summary>
    public string? GetAutoStartPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, writable: false);
            return key?.GetValue(APPLICATION_NAME) as string;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get auto-start path: {ex.Message}");
            return null;
        }
    }
}