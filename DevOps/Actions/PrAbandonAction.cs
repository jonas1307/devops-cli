using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class PrAbandonAction
{
    internal static async Task<int> Execute(PrAbandonOptions opts, CancellationToken ct)
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

            var updated = await HttpService.SetPullRequestStatus(project, repo, opts.Id, "abandoned", ct);
            ConsoleHelper.WriteSuccess($"Pull request #{opts.Id} abandoned (status: {updated.Status}).");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
