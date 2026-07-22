using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class ListAction
{
    internal static async Task<int> Execute(ListOptions opts, CancellationToken ct)
    {
        try
        {
            var project = ConfigService.ResolveProject(opts.Project);
            var items = await HttpService.ListWorkItems(project, opts.State, opts.Type, opts.AssignedTo, opts.Query, opts.ParentId, ct);

            if (items.Count == 0)
            {
                Console.WriteLine("No work items found.");
                return 0;
            }

            var idWidth = items.Max(x => x.Id.ToString().Length);
            var typeWidth = Math.Min(items.Max(x => (x.Fields.WorkItemType ?? "").Length), 12);
            var stateWidth = Math.Min(items.Max(x => (x.Fields.State ?? "").Length), 12);
            var assigneeWidth = 20;
            var titleWidth = Console.WindowWidth - idWidth - typeWidth + stateWidth + assigneeWidth + 12;
            titleWidth = Math.Clamp(titleWidth, 20, 60);

            Console.WriteLine($"{"ID".PadLeft(idWidth)}  {"TYPE".PadRight(typeWidth)}  {"STATE".PadRight(stateWidth)}  {"ASSIGNED TO".PadRight(assigneeWidth)}  TITLE");
            Console.WriteLine(new string('-', idWidth + typeWidth + stateWidth + assigneeWidth + titleWidth + 10));

            foreach (var item in items)
            {
                var id = item.Id.ToString().PadLeft(idWidth);
                var type = ActionHelpers.Truncate(item.Fields.WorkItemType, typeWidth).PadRight(typeWidth);
                var state = ActionHelpers.Truncate(item.Fields.State, stateWidth).PadRight(stateWidth);
                var assignee = ActionHelpers.Truncate(item.Fields.AssignedTo?.DisplayName ?? "-", assigneeWidth).PadRight(assigneeWidth);
                var title = ActionHelpers.Truncate(item.Fields.Title, titleWidth);
                Console.WriteLine($"{id}  {type}  {state}  {assignee}  {title}");
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
