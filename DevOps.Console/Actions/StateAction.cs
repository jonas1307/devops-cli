using DevOps.Options;
using DevOps.Requests;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class StateAction
{
    internal static async Task<int> Execute(StateOptions opts, CancellationToken ct)
    {
        var project = ConfigService.ResolveProject(opts.Project);
        var operations = new List<JsonPatchOperation>
        {
            new() { Op = "add", Path = "/fields/System.State", Value = opts.State }
        };

        var tasks = opts.Ids.Select(async id =>
        {
            try
            {
                var current = await HttpService.GetWorkItem(id, project, ct);
                var previousState = current.Fields.State;
                var item = await HttpService.UpdateWorkItem(id, project, operations, ct);
                ConsoleHelper.WriteSuccess($"Work item #{item.Id}: {previousState} -> {item.Fields.State}");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error on #{id}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
        return 0;
    }
}
