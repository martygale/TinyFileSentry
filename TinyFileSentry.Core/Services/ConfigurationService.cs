using System.Text.Json;
using TinyFileSentry.Core.Extensions;
using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Models;

namespace TinyFileSentry.Core.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogService _logService;
    private readonly string _configPath;

    public ConfigurationService(IFileSystem fileSystem, ILogService logService)
    {
        _fileSystem = fileSystem;
        _logService = logService;
        _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "TinyFileSentry", "config.json");
    }

    public virtual string GetConfigurationPath() => _configPath;

    public Configuration LoadConfiguration()
    {
        try
        {
            var configPath = GetConfigurationPath();
            if (!_fileSystem.FileExists(configPath))
            {
                _logService.Info("Configuration file not found, creating default configuration", nameof(ConfigurationService));
                var defaultConfig = new Configuration();
                SaveConfiguration(defaultConfig);
                return defaultConfig;
            }

            var json = _fileSystem.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<Configuration>(json);
            
            if (config == null)
            {
                _logService.Warning("Configuration file is empty or invalid, using default configuration", nameof(ConfigurationService));
                return new Configuration();
            }

            // Validate PollingSpeed (all enum values are valid, but just in case)
            if (!Enum.IsDefined(typeof(PollingSpeed), config.PollingSpeed))
            {
                _logService.Warning($"Invalid polling speed {config.PollingSpeed}, setting to Fast", nameof(ConfigurationService));
                config.PollingSpeed = PollingSpeed.Fast;
            }

            _logService.Info($"Configuration loaded successfully with {config.WatchRules.Count} watch rules", nameof(ConfigurationService));
            return config;
        }
        catch (Exception ex)
        {
            _logService.Error($"Failed to load configuration: {ex.Message}", nameof(ConfigurationService));
            return new Configuration();
        }
    }

    public void SaveConfiguration(Configuration configuration)
    {
        try
        {
            var configPath = GetConfigurationPath();
            var directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory) && !_fileSystem.DirectoryExists(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(configuration, options);
            _fileSystem.WriteAllText(configPath, json);
            
            _logService.Info("Configuration saved successfully", nameof(ConfigurationService));
        }
        catch (Exception ex)
        {
            _logService.Error($"Failed to save configuration: {ex.Message}", nameof(ConfigurationService));
        }
    }
}