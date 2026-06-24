using DevOps.Options;
using DevOps.Requests;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class CommentAction
{
    internal static async Task<int> Execute(CommentOptions opts, CancellationToken ct)
    {
        try
        {
            var project = ConfigService.ResolveProject(opts.Project);

            var operations = new List<JsonPatchOperation>
            {
                new() { Op = "add", Path = "/fields/System.History", Value = opts.Message }
            };

            var item = await HttpService.UpdateWorkItem(opts.Id, project, operations, ct);
            ConsoleHelper.WriteSuccess($"Comment added to work item #{item.Id}: {item.Fields.Title}");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
