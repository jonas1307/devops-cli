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
            var prs = await HttpService.ListPullRequests(project, opts.Repo, status, opts.Target, opts.Top, ct);

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
                    Markup.Escape(title),
                    ActionHelpers.ColorState(pr.Status),
                    Markup.Escape(ActionHelpers.ShortBranch(pr.SourceRefName)),
                    Markup.Escape(ActionHelpers.ShortBranch(pr.TargetRefName)),
                    Markup.Escape(pr.CreatedBy?.DisplayName ?? "-"));
            }

            AnsiConsole.Write(table);
            ActionHelpers.WriteFooter($"Total: {prs.Count} pull request(s)");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
