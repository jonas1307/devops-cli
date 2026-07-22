using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;
using Spectre.Console;

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

            var table = ActionHelpers.NewTable("ID", "TYPE", "STATE", "ASSIGNED TO", "TITLE");

            foreach (var item in items)
            {
                table.AddRow(
                    Markup.Escape(item.Id.ToString()),
                    Markup.Escape(item.Fields.WorkItemType ?? "-"),
                    ActionHelpers.ColorState(item.Fields.State),
                    Markup.Escape(item.Fields.AssignedTo?.DisplayName ?? "-"),
                    Markup.Escape(item.Fields.Title ?? "-"));
            }

            AnsiConsole.Write(table);
            ActionHelpers.WriteFooter($"Total: {items.Count} work item(s)");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
