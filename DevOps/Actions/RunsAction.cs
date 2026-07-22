using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

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

            var idWidth = Math.Max(recent.Max(r => r.Id.ToString().Length), 2);
            var nameWidth = Math.Min(Math.Max(recent.Max(r => (r.Name ?? "").Length), 4), 24);
            var stateWidth = 11;
            var resultWidth = 10;

            Console.WriteLine($"{"ID".PadLeft(idWidth)}  {"NAME".PadRight(nameWidth)}  {"STATE".PadRight(stateWidth)}  {"RESULT".PadRight(resultWidth)}  CREATED");
            Console.WriteLine(new string('-', idWidth + nameWidth + stateWidth + resultWidth + 22));

            foreach (var r in recent)
            {
                var id = r.Id.ToString().PadLeft(idWidth);
                var name = ActionHelpers.Truncate(r.Name ?? "-", nameWidth).PadRight(nameWidth);
                var state = ActionHelpers.Truncate(r.State ?? "-", stateWidth).PadRight(stateWidth);
                var runResult = ActionHelpers.Truncate(string.IsNullOrEmpty(r.Result) ? "-" : r.Result, resultWidth).PadRight(resultWidth);
                var created = r.CreatedDate.HasValue ? r.CreatedDate.Value.ToString("yyyy/MM/dd HH:mm") : "-";
                Console.WriteLine($"{id}  {name}  {state}  {runResult}  {created}");
            }

            Console.WriteLine();
            Console.WriteLine($"Showing {recent.Count} of {runs.Count} run(s) for pipeline {opts.PipelineId}.");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
