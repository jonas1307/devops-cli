using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class PrVoteAction
{
    internal static async Task<int> Execute(PrVoteOptions opts, CancellationToken ct)
    {
        try
        {
            var vote = ActionHelpers.VoteValue(opts.Vote);
            var userId = ConfigService.ResolveUserId();

            var pr = await HttpService.GetPullRequest(opts.Id, ct);
            var repo = pr.Repository?.Name;
            var project = pr.Repository?.Project?.Name;
            if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(project))
            {
                ConsoleHelper.WriteError($"Could not resolve the repository for pull request {opts.Id}.");
                return 1;
            }

            await HttpService.VotePullRequest(project, repo, opts.Id, userId, vote, ct);
            ConsoleHelper.WriteSuccess($"Voted '{ActionHelpers.VoteText(vote)}' on pull request #{opts.Id}.");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
