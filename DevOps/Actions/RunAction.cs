using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class RunAction
{
    internal static async Task<int> Execute(RunOptions opts, CancellationToken ct)
    {
        try
        {
            var project = ConfigService.ResolveProject(opts.Project);
            var run = await HttpService.QueuePipelineRun(project, opts.PipelineId, opts.Branch, ct);

            var branchInfo = string.IsNullOrEmpty(opts.Branch) ? "default branch" : opts.Branch;
            ConsoleHelper.WriteSuccess($"Pipeline {opts.PipelineId} run queued on {branchInfo}: #{run.Id} ({run.State})");

            var url = run.Links?.Web?.Href;
            if (!string.IsNullOrEmpty(url))
                Console.WriteLine($"URL: {url}");

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
