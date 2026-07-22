using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

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

            var idWidth = Math.Max(pipelines.Max(p => p.Id.ToString().Length), 2);
            var nameWidth = Math.Min(pipelines.Max(p => p.Name.Length), 50);

            Console.WriteLine($"{"ID".PadLeft(idWidth)}  {"NAME".PadRight(nameWidth)}  FOLDER");
            Console.WriteLine(new string('-', idWidth + nameWidth + 20));

            foreach (var p in pipelines.OrderBy(p => p.Folder).ThenBy(p => p.Name))
            {
                var folder = string.IsNullOrWhiteSpace(p.Folder) || p.Folder == "\\" ? "-" : p.Folder;
                Console.WriteLine($"{p.Id.ToString().PadLeft(idWidth)}  {ActionHelpers.Truncate(p.Name, nameWidth).PadRight(nameWidth)}  {folder}");
            }

            Console.WriteLine();
            Console.WriteLine($"Total: {pipelines.Count} pipeline(s)");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
