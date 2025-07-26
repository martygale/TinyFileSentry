namespace TinyFileSentry.Core.Interfaces;

public interface IPostAction
{
    Task<bool> ExecuteAsync(string filePath, string destinationDirectory, string sourceFilePath, CancellationToken cancellationToken = default);
}