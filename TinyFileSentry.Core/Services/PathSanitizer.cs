using TinyFileSentry.Core.Interfaces;

namespace TinyFileSentry.Core.Services;

public class PathSanitizer : IPathSanitizer
{
    private readonly IFileSystem _fileSystem;

    public PathSanitizer(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public string SanitizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        var sanitized = path.Replace(":\\", "_").Replace("\\", "_");
        
        var invalidChars = _fileSystem.GetInvalidFileNameChars();
        foreach (var invalidChar in invalidChars)
        {
            sanitized = sanitized.Replace(invalidChar, "_");
        }

        // Приводим путь к нижнему регистру для консистентности
        sanitized = sanitized.ToLowerInvariant();
        
        // Ограничиваем длину для кроссплатформенной совместимости
        if (sanitized.Length > 200)
        {
            sanitized = sanitized.Substring(0, 200);
        }

        return sanitized;
    }
}