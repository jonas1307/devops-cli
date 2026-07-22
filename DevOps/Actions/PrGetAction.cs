using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class PrGetAction
{
    internal static async Task<int> Execute(PrGetOptions opts, CancellationToken ct)
    {
        try
        {
            var pr = await HttpService.GetPullRequest(opts.Id, ct);

            Console.WriteLine($"ID       : {pr.PullRequestId}");
            Console.WriteLine($"Title    : {pr.Title}{(pr.IsDraft ? " (draft)" : "")}");
            Console.WriteLine($"Status   : {pr.Status}");
            Console.WriteLine($"Author   : {pr.CreatedBy?.DisplayName ?? "-"}");
            Console.WriteLine($"Source   : {ActionHelpers.ShortBranch(pr.SourceRefName)}");
            Console.WriteLine($"Target   : {ActionHelpers.ShortBranch(pr.TargetRefName)}");
            Console.WriteLine($"Repo     : {pr.Repository?.Name ?? "-"}");
            Console.WriteLine($"Created  : {(pr.CreationDate.HasValue ? pr.CreationDate.Value.ToString("yyyy-MM-dd HH:mm") : "-")}");

            if (pr.Reviewers is { Count: > 0 })
            {
                Console.WriteLine("Reviewers:");
                foreach (var r in pr.Reviewers)
                {
                    var required = r.IsRequired ? " (required)" : "";
                    Console.WriteLine($"  - {r.DisplayName} [{ActionHelpers.VoteText(r.Vote)}]{required}");
                }
            }

            Console.WriteLine($"URL      : {ActionHelpers.ResolvePullRequestUrl(pr) ?? "-"}");

            if (!string.IsNullOrWhiteSpace(pr.Description))
            {
                Console.WriteLine();
                Console.WriteLine("Description:");
                Console.WriteLine(pr.Description);
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
