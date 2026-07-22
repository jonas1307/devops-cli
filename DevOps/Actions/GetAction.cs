using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class GetAction
{
    internal static async Task<int> Execute(GetOptions opts, CancellationToken ct)
    {
        try
        {
            var project = ConfigService.ResolveProject(opts.Project);
            var item = await HttpService.GetWorkItem(opts.Id, project, ct);

            Console.WriteLine($"ID      : {item.Id}");
            Console.WriteLine($"Type    : {item.Fields.WorkItemType}");
            Console.WriteLine($"Title   : {item.Fields.Title}");
            Console.WriteLine($"State   : {item.Fields.State}");
            Console.WriteLine($"Assigned: {item.Fields.AssignedTo?.DisplayName ?? "(unassigned)"}");
            Console.WriteLine($"Priority: {item.Fields.Priority?.ToString() ?? "-"}");
            Console.WriteLine($"Created : {item.Fields.CreatedDate:yyyy-MM-dd}");
            Console.WriteLine($"Changed : {item.Fields.ChangedDate:yyyy-MM-dd}");
            Console.WriteLine($"Project : {item.Fields.TeamProject}");
            Console.WriteLine($"URL     : {item.Url?.Replace("_apis/wit/workitems", "_workitems/edit")}");

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
