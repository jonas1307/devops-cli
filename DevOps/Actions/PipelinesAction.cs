using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;
using Spectre.Console;

namespace DevOps.Actions;

internal static class PipelinesAction
{
    internal static async Task<int> Execute(PipelinesOptions opts, CancellationToken ct)
    {
        try
        {
            var project = ConfigService.ResolveProject(opts.Project);
            var pipelines = await HttpService.ListPipelines(project, opts.Name, ct);

            if (pipelines.Count == 0)
            {
                Console.WriteLine("No pipelines found.");
                return 0;
            }

            var table = ActionHelpers.NewTable("ID", "NAME", "FOLDER");

            foreach (var p in pipelines.OrderBy(p => p.Folder).ThenBy(p => p.Name))
            {
                var folder = string.IsNullOrWhiteSpace(p.Folder) || p.Folder == "\\" ? "-" : p.Folder;
                table.AddRow(
                    Markup.Escape(p.Id.ToString()),
                    Markup.Escape(ActionHelpers.Truncate(p.Name ?? "-", 45)),
                    Markup.Escape(ActionHelpers.Truncate(folder, 40)));
            }

            AnsiConsole.Write(table);
            ActionHelpers.WriteFooter($"Total: {pipelines.Count} pipeline(s)");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
