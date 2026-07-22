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
            var pr = await HttpService.CreatePullRequest(project, opts.Repo, opts.Source, opts.Target, opts.Title, opts.Description, opts.Draft, ct);

            ConsoleHelper.WriteSuccess($"Pull request #{pr.PullRequestId} created{(pr.IsDraft ? " (draft)" : "")}: {pr.Title}");

            var url = pr.Links?.Web?.Href;
            if (!string.IsNullOrEmpty(url))
                Console.WriteLine($"URL: {url}");

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
