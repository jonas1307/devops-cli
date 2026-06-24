using System.Text.RegularExpressions;
using DevOps.Options;
using DevOps.Requests;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class NormalizeAction
{
    private static readonly Regex Unnormalized = new(@"^\[.+\] .+");
    private static readonly Regex AlreadyNormalized = new(@"^[A-Z]+ \d+ - ");

    internal static async Task<int> Execute(NormalizeOptions opts, CancellationToken ct)
    {
        try
        {
            var project = ConfigService.ResolveProject(opts.Project);
            var items = await HttpService.ListWorkItems(project, opts.State, "Task", null, null, opts.ParentId, ct);

            var toNormalize = items
                .Where(i => Unnormalized.IsMatch(i.Fields.Title ?? "") && !AlreadyNormalized.IsMatch(i.Fields.Title ?? ""))
                .ToList();

            if (toNormalize.Count == 0)
            {
                Console.WriteLine("No items to normalize.");
                return 0;
            }

            if (opts.DryRun)
                Console.WriteLine($"[dry-run] {toNormalize.Count} item(s) would be renamed:\n");
            else
                Console.WriteLine($"Normalizing {toNormalize.Count} item(s)...\n");

            var parentCache = new Dictionary<int, string>();

            foreach (var item in toNormalize)
            {
                if (item.Fields.ParentId is null)
                {
                    ConsoleHelper.WriteError($"#{item.Id} '{item.Fields.Title}' has no parent — skipped.");
                    continue;
                }

                var parentId = item.Fields.ParentId.Value;

                if (!parentCache.TryGetValue(parentId, out var abbrev))
                {
                    var parent = await HttpService.GetWorkItem(parentId, project, ct);
                    abbrev = parent.Fields.WorkItemType == "Product Backlog Item"
                        ? "PBI"
                        : parent.Fields.WorkItemType?.ToUpperInvariant() ?? "UNKNOWN";
                    parentCache[parentId] = abbrev;
                }

                var newTitle = $"{abbrev} {parentId} - {item.Fields.Title}";

                if (opts.DryRun)
                {
                    Console.WriteLine($"  #{item.Id}: '{item.Fields.Title}'");
                    Console.WriteLine($"        → '{newTitle}'");
                    continue;
                }

                var operations = new List<JsonPatchOperation>
                {
                    new() { Op = "add", Path = "/fields/System.Title", Value = newTitle }
                };

                await HttpService.UpdateWorkItem(item.Id, project, operations, ct);
                ConsoleHelper.WriteSuccess($"#{item.Id}: renamed to '{newTitle}'");
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
