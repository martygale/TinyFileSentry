using TinyFileSentry.Core.Interfaces;

namespace TinyFileSentry.Core.Services.PostActions;

public class NonePostAction : IPostAction
{
    public Task<bool> ExecuteAsync(string filePath, string destinationDirectory, string sourceFilePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}