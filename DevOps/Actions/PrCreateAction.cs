using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class PrCreateAction
{
    internal static async Task<int> Execute(PrCreateOptions opts, CancellationToken ct)
    {
        try
        {
            var project = ConfigService.ResolveProject(opts.Project);

            var source = opts.Source;
            if (string.IsNullOrWhiteSpace(source))
            {
                source = GitHelper.CurrentBranch();
                if (string.IsNullOrWhiteSpace(source))
                {
                    ConsoleHelper.WriteError("Could not detect the current git branch. Provide --source explicitly.");
                    return 1;
                }

                ActionHelpers.WriteMuted($"Using current branch as source: {source}");
            }

            List<string> reviewerIds = null;
            if (opts.Reviewers != null && opts.Reviewers.Any())
                reviewerIds = await HttpService.ResolveReviewerIds(opts.Reviewers, ct);

            var pr = await HttpService.CreatePullRequest(project, opts.Repo, source, opts.Target, opts.Title, opts.Description, opts.Draft, reviewerIds, ct);

            ConsoleHelper.WriteSuccess($"Pull request #{pr.PullRequestId} created{(pr.IsDraft ? " (draft)" : "")}: {pr.Title}");

            var url = ActionHelpers.ResolvePullRequestUrl(pr);
            if (!string.IsNullOrEmpty(url))
                Console.WriteLine($"URL: {url}");

            var workItems = opts.WorkItems?.ToList();
            if (workItems is { Count: > 0 })
            {
                await HttpService.LinkWorkItemsToPullRequest(pr.Repository.Project.Id, pr.Repository.Id, pr.PullRequestId, workItems, project, ct);
                ActionHelpers.WriteMuted($"Linked work item(s): {string.Join(", ", workItems.Select(id => $"#{id}"))}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
