using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class MineAction
{
    internal static async Task<int> Execute(MineOptions opts, CancellationToken ct)
    {
        try
        {
            var project = ConfigService.ResolveProject(opts.Project);
            var items = await HttpService.ListWorkItems(project, opts.State, opts.Type, "me", opts.Query, opts.ParentId, ct);

            if (items.Count == 0)
            {
                Console.WriteLine("No work items assigned to you.");
                return 0;
            }

            var idWidth = items.Max(x => x.Id.ToString().Length);
            var typeWidth = Math.Min(items.Max(x => (x.Fields.WorkItemType ?? "").Length), 12);
            var stateWidth = Math.Min(items.Max(x => (x.Fields.State ?? "").Length), 12);
            var titleWidth = Console.WindowWidth - idWidth - typeWidth - stateWidth - 8;
            titleWidth = Math.Clamp(titleWidth, 20, 70);

            Console.WriteLine($"{"ID".PadLeft(idWidth)}  {"TYPE".PadRight(typeWidth)}  {"STATE".PadRight(stateWidth)}  TITLE");
            Console.WriteLine(new string('-', idWidth + typeWidth + stateWidth + titleWidth + 8));

            foreach (var item in items)
            {
                var id = item.Id.ToString().PadLeft(idWidth);
                var type = ActionHelpers.Truncate(item.Fields.WorkItemType, typeWidth).PadRight(typeWidth);
                var state = ActionHelpers.Truncate(item.Fields.State, stateWidth).PadRight(stateWidth);
                var title = ActionHelpers.Truncate(item.Fields.Title, titleWidth);
                Console.WriteLine($"{id}  {type}  {state}  {title}");
            }

            Console.WriteLine();
            Console.WriteLine($"Total: {items.Count} work item(s)");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
