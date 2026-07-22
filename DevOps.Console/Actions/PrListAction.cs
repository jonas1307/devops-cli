using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class PrListAction
{
    internal static async Task<int> Execute(PrListOptions opts, CancellationToken ct)
    {
        try
        {
            var project = ConfigService.ResolveProject(opts.Project);
            var status = string.Equals(opts.Status, "all", StringComparison.OrdinalIgnoreCase) ? null : opts.Status;
            var prs = await HttpService.ListPullRequests(project, opts.Repo, status, opts.Target, opts.Top, ct);

            if (prs.Count == 0)
            {
                Console.WriteLine("No pull requests found.");
                return 0;
            }

            var idWidth = Math.Max(prs.Max(p => p.PullRequestId.ToString().Length), 2);
            var titleWidth = 40;
            var statusWidth = 9;
            var branchWidth = 18;
            var authorWidth = 18;

            Console.WriteLine($"{"ID".PadLeft(idWidth)}  {"TITLE".PadRight(titleWidth)}  {"STATUS".PadRight(statusWidth)}  {"SOURCE -> TARGET".PadRight(branchWidth * 2 + 4)}  AUTHOR");
            Console.WriteLine(new string('-', idWidth + titleWidth + statusWidth + branchWidth * 2 + authorWidth + 14));

            foreach (var pr in prs)
            {
                var id = pr.PullRequestId.ToString().PadLeft(idWidth);
                var title = ActionHelpers.Truncate((pr.IsDraft ? "[draft] " : "") + (pr.Title ?? "-"), titleWidth).PadRight(titleWidth);
                var status2 = ActionHelpers.Truncate(pr.Status ?? "-", statusWidth).PadRight(statusWidth);
                var src = ActionHelpers.Truncate(ActionHelpers.ShortBranch(pr.SourceRefName), branchWidth);
                var tgt = ActionHelpers.Truncate(ActionHelpers.ShortBranch(pr.TargetRefName), branchWidth);
                var branches = $"{src} -> {tgt}".PadRight(branchWidth * 2 + 4);
                var author = ActionHelpers.Truncate(pr.CreatedBy?.DisplayName ?? "-", authorWidth);
                Console.WriteLine($"{id}  {title}  {status2}  {branches}  {author}");
            }

            Console.WriteLine();
            Console.WriteLine($"Total: {prs.Count} pull request(s)");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
