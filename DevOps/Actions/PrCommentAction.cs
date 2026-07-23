using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class PrCommentAction
{
    internal static async Task<int> Execute(PrCommentOptions opts, CancellationToken ct)
    {
        try
        {
            var pr = await HttpService.GetPullRequest(opts.Id, ct);
            var repo = pr.Repository?.Name;
            var project = pr.Repository?.Project?.Name;
            if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(project))
            {
                ConsoleHelper.WriteError($"Could not resolve the repository for pull request {opts.Id}.");
                return 1;
            }

            await HttpService.AddPullRequestComment(project, repo, opts.Id, opts.Message, ct);
            ConsoleHelper.WriteSuccess($"Comment added to pull request #{opts.Id}.");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
