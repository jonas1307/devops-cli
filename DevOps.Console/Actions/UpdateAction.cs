using DevOps.Options;
using DevOps.Requests;
using DevOps.Services;
using DevOps.Utils;
using DevOps.Validators;

namespace DevOps.Actions;

internal static class UpdateAction
{
    internal static async Task<int> Execute(UpdateOptions opts, CancellationToken ct)
    {
        var validator = new UpdateValidator();
        var validation = validator.Validate(opts);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ConsoleHelper.WriteError(error.ErrorMessage);
            return 1;
        }

        try
        {
            var project = ConfigService.ResolveProject(opts.Project);

            var operations = new List<JsonPatchOperation>();

            if (!string.IsNullOrEmpty(opts.Title))
                operations.Add(new() { Op = "add", Path = "/fields/System.Title", Value = opts.Title });

            if (!string.IsNullOrEmpty(opts.State))
                operations.Add(new() { Op = "add", Path = "/fields/System.State", Value = opts.State });

            if (!string.IsNullOrEmpty(opts.AssignedTo))
                operations.Add(new() { Op = "add", Path = "/fields/System.AssignedTo", Value = ConfigService.ResolveAssignedTo(opts.AssignedTo) });

            if (!string.IsNullOrEmpty(opts.Description))
                operations.Add(new() { Op = "add", Path = "/fields/System.Description", Value = opts.Description });

            if (opts.Priority.HasValue)
                operations.Add(new() { Op = "add", Path = "/fields/Microsoft.VSTS.Common.Priority", Value = opts.Priority.Value });

            if (!string.IsNullOrEmpty(opts.Iteration))
                operations.Add(new() { Op = "add", Path = "/fields/System.IterationPath", Value = opts.Iteration });

            if (!string.IsNullOrEmpty(opts.Area))
                operations.Add(new() { Op = "add", Path = "/fields/System.AreaPath", Value = opts.Area });

            if (opts.Estimate.HasValue)
                operations.Add(new() { Op = "add", Path = "/fields/Custom.EstimateWork", Value = opts.Estimate.Value });

            if (!string.IsNullOrEmpty(opts.ActivityType))
                operations.Add(new() { Op = "add", Path = "/fields/Microsoft.VSTS.Common.Activity", Value = opts.ActivityType });

            foreach (var field in opts.Fields ?? [])
            {
                var sep = field.IndexOf('=');
                var key = field[..sep];
                var value = field[(sep + 1)..];
                operations.Add(new() { Op = "add", Path = $"/fields/{key}", Value = value });
            }

            if (!string.IsNullOrEmpty(opts.Comment))
                operations.Add(new() { Op = "add", Path = "/fields/System.History", Value = opts.Comment });

            if (opts.RelatedId.HasValue)
            {
                var relType = ActionHelpers.ResolveRelationType(opts.RelationType);
                var config = ConfigService.LoadConfig();
                var relUrl = $"{config.OrgUrl.TrimEnd('/')}/_apis/wit/workitems/{opts.RelatedId.Value}";
                operations.Add(new()
                {
                    Op = "add",
                    Path = "/relations/-",
                    Value = new WorkItemRelation { Rel = relType, Url = relUrl }
                });
            }

            var item = await HttpService.UpdateWorkItem(opts.Id, project, operations, ct);
            ConsoleHelper.WriteSuccess($"Work item #{item.Id} updated: {item.Fields.Title}");
            if (opts.RelatedId.HasValue)
                Console.WriteLine($"Linked to #{opts.RelatedId.Value} as '{opts.RelationType}'.");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
