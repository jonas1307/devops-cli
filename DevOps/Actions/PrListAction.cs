using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;
using Spectre.Console;

namespace DevOps.Actions;

internal static class PrListAction
{
    internal static async Task<int> Execute(PrListOptions opts, CancellationToken ct)
    {
        try
        {
            var project = ConfigService.ResolveProject(opts.Project);
            var status = string.Equals(opts.Status, "all", StringComparison.OrdinalIgnoreCase) ? null : opts.Status;
            var creatorId = opts.Mine ? ConfigService.ResolveUserId() : null;
            var prs = await HttpService.ListPullRequests(project, opts.Repo, status, opts.Target, opts.Top, creatorId, ct);

            if (prs.Count == 0)
            {
                Console.WriteLine("No pull requests found.");
                return 0;
            }

            var table = ActionHelpers.NewTable("ID", "TITLE", "STATUS", "SOURCE", "TARGET", "AUTHOR");

            foreach (var pr in prs)
            {
                var title = (pr.IsDraft ? "[draft] " : "") + (pr.Title ?? "-");
                table.AddRow(
                    Markup.Escape(pr.PullRequestId.ToString()),
                    Markup.Escape(ActionHelpers.Truncate(title, 50)),
                    ActionHelpers.ColorState(pr.Status),
                    Markup.Escape(ActionHelpers.Truncate(ActionHelpers.ShortBranch(pr.SourceRefName), 24)),
                    Markup.Escape(ActionHelpers.Truncate(ActionHelpers.ShortBranch(pr.TargetRefName), 24)),
                    Markup.Escape(ActionHelpers.Truncate(pr.CreatedBy?.DisplayName ?? "-", 22)));
            }

            AnsiConsole.Write(table);
            ActionHelpers.WriteMuted($"Total: {prs.Count} pull request(s)");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
