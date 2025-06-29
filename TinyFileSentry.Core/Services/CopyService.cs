using TinyFileSentry.Core.Interfaces;

namespace TinyFileSentry.Core.Services;

public class CopyService : ICopyService
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogService _logService;
    private readonly IPathSanitizer _pathSanitizer;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxRetryAttempts = 10; // каждые 0.5 с = 5 секунд максимум
    private const int RetryDelayMs = 500;

    public CopyService(IFileSystem fileSystem, ILogService logService, IPathSanitizer pathSanitizer)
    {
        _fileSystem = fileSystem;
        _logService = logService;
        _pathSanitizer = pathSanitizer;
    }

    public async Task<bool> CopyFileAsync(string sourcePath, string destinationRoot, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_fileSystem.FileExists(sourcePath))
            {
                _logService.Warning($"Source file not found: {sourcePath}", nameof(CopyService));
                return false;
            }

            var fileSize = _fileSystem.GetFileSize(sourcePath);
            if (fileSize > MaxFileSizeBytes)
            {
                _logService.Warning($"File size {fileSize / 1024 / 1024} MB exceeds limit, skipping: {sourcePath}", nameof(CopyService));
                return false;
            }

            var fileName = Path.GetFileName(sourcePath);
            var sourceDirectory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
            var sanitizedPath = _pathSanitizer.SanitizePath(sourceDirectory);
            
            var destinationDirectory = Path.Combine(destinationRoot, sanitizedPath);
            var destinationPath = Path.Combine(destinationDirectory, fileName);

            if (!_fileSystem.DirectoryExists(destinationDirectory))
            {
                _fileSystem.CreateDirectory(destinationDirectory);
            }

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    _fileSystem.CopyFile(sourcePath, destinationPath, overwrite: true);
                    _logService.Info($"File copied successfully: {sourcePath} -> {destinationPath}", nameof(CopyService));
                    return true;
                }
                catch (IOException ex) when (attempt < MaxRetryAttempts)
                {
                    _logService.Warning($"Copy attempt {attempt} failed for {sourcePath}: {ex.Message}. Retrying...", nameof(CopyService));
                    await Task.Delay(RetryDelayMs, cancellationToken);
                }
            }

            _logService.Error($"Failed to copy file after {MaxRetryAttempts} attempts: {sourcePath}", nameof(CopyService));
            return false;
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error copying file {sourcePath}: {ex.Message}", nameof(CopyService));
            return false;
        }
    }
}