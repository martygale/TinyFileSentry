namespace TinyFileSentry.Core.Interfaces;

public interface ICopyService
{
    Task<bool> CopyFileAsync(string sourcePath, string destinationRoot, CancellationToken cancellationToken = default);
}