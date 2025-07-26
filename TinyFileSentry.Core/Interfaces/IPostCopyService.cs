using TinyFileSentry.Core.Models;

namespace TinyFileSentry.Core.Interfaces;

public interface IPostCopyService
{
    Task<bool> ExecutePostActionAsync(PostActionType actionType, string filePath, string destinationDirectory, string sourceFilePath, CancellationToken cancellationToken = default);
}