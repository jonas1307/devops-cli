using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;
using Spectre.Console;

namespace DevOps.Actions;

internal static class RunsAction
{
    internal static async Task<int> Execute(RunsOptions opts, CancellationToken ct)
    {
        try
        {
            var project = ConfigService.ResolveProject(opts.Project);
            var runs = await HttpService.ListPipelineRuns(project, opts.PipelineId, ct);

            if (runs.Count == 0)
            {
                Console.WriteLine("No runs found for this pipeline.");
                return 0;
            }

            var recent = runs
                .OrderByDescending(r => r.CreatedDate ?? DateTime.MinValue)
                .Take(Math.Max(opts.Top, 1))
                .ToList();

            var table = ActionHelpers.NewTable("ID", "NAME", "STATE", "RESULT", "CREATED");

            foreach (var r in recent)
            {
                var created = r.CreatedDate.HasValue ? r.CreatedDate.Value.ToString("yyyy/MM/dd HH:mm") : "-";
                table.AddRow(
                    Markup.Escape(r.Id.ToString()),
                    Markup.Escape(ActionHelpers.Truncate(r.Name ?? "-", 30)),
                    ActionHelpers.ColorState(r.State),
                    ActionHelpers.ColorResult(r.Result),
                    Markup.Escape(created));
            }

            AnsiConsole.Write(table);
            ActionHelpers.WriteFooter($"Showing {recent.Count} of {runs.Count} run(s) for pipeline {opts.PipelineId}.");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
