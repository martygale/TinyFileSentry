using TinyFileSentry.Core.Models;

namespace TinyFileSentry.Core.Interfaces;

public interface IConfigurationService
{
    Configuration LoadConfiguration();
    void SaveConfiguration(Configuration configuration);
    string GetConfigurationPath();
}