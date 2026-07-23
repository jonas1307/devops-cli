using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class DeleteAction
{
    internal static async Task<int> Execute(DeleteOptions opts, CancellationToken ct)
    {
        try
        {
            var project = ConfigService.ResolveProject(opts.Project);
            var ids = opts.Ids.ToList();

            if (!opts.Force)
            {
                Console.WriteLine($"About to delete {ids.Count} work item(s): {string.Join(", ", ids.Select(i => "#" + i))}");
                Console.Write("They will be moved to the recycle bin. Continue? [y/N] ");
                var answer = Console.ReadLine();
                if (!string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Aborted.");
                    return 0;
                }
            }

            var tasks = ids.Select(async id =>
            {
                try
                {
                    await HttpService.DeleteWorkItem(id, project, ct);
                    ConsoleHelper.WriteSuccess($"Deleted work item #{id}");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Error on #{id}: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
