using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Models;
using TinyFileSentry.Core.Services.PostActions;

namespace TinyFileSentry.Core.Services;

public class PostCopyService : IPostCopyService
{
    private readonly Dictionary<PostActionType, IPostAction> _postActions;

    public PostCopyService(IProcessRunner processRunner, ILogService logService, IFileSystem fileSystem)
    {
        _postActions = new Dictionary<PostActionType, IPostAction>
        {
            { PostActionType.None, new NonePostAction() },
            { PostActionType.GitCommit, new GitCommitPostAction(processRunner, logService, fileSystem) },
            { PostActionType.GitCommitAndPush, new GitCommitAndPushPostAction(processRunner, logService, fileSystem) }
        };
    }

    public async Task<bool> ExecutePostActionAsync(PostActionType actionType, string filePath, string destinationDirectory, string sourceFilePath, CancellationToken cancellationToken = default)
    {
        if (_postActions.TryGetValue(actionType, out var postAction))
        {
            return await postAction.ExecuteAsync(filePath, destinationDirectory, sourceFilePath, cancellationToken);
        }

        return false;
    }
}