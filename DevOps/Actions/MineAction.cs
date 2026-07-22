using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;
using Spectre.Console;

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

            var table = ActionHelpers.NewTable("ID", "TYPE", "STATE", "TITLE");

            foreach (var item in items)
            {
                table.AddRow(
                    Markup.Escape(item.Id.ToString()),
                    Markup.Escape(item.Fields.WorkItemType ?? "-"),
                    ActionHelpers.ColorState(item.Fields.State),
                    Markup.Escape(ActionHelpers.Truncate(item.Fields.Title ?? "-", 70)));
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
