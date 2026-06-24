using DevOps.Options;
using DevOps.Requests;
using DevOps.Responses;
using DevOps.Services;
using DevOps.Utils;
using DevOps.Validators;

namespace DevOps.Actions;

internal static class CreateAction
{
    internal static async Task<int> Execute(CreateOptions opts, CancellationToken ct)
    {
        var validator = new CreateValidator();
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
            var team = ConfigService.ResolveTeam(project);

            var needIteration = string.IsNullOrEmpty(opts.Iteration);
            var needArea = string.IsNullOrEmpty(opts.Area);

            string iterationPath;
            string iterationName;
            string areaPath;

            var cache = (needIteration || needArea) ? CacheService.Load() : null;

            if (cache != null && cache.IsValid())
            {
                iterationPath = needIteration ? cache.IterationPath : opts.Iteration;
                iterationName = needIteration ? cache.IterationName : null;
                areaPath = needArea ? cache.AreaPath : opts.Area;
            }
            else
            {
                var iterationTask = needIteration ? HttpService.GetCurrentIteration(project, team, ct) : Task.FromResult<IterationResponse>(null);
                var areaTask = needArea ? HttpService.GetDefaultAreaPath(project, team, ct) : Task.FromResult<string>(null);
                await Task.WhenAll(iterationTask, areaTask);

                iterationPath = needIteration ? iterationTask.Result.Path : opts.Iteration;
                iterationName = needIteration ? iterationTask.Result.Name : null;
                areaPath = needArea ? areaTask.Result : opts.Area;

                if (needIteration || needArea)
                {
                    CacheService.Save(new TeamCache
                    {
                        IterationPath = iterationPath,
                        IterationName = iterationName,
                        AreaPath = areaPath,
                        CachedAt = DateTime.Now
                    });
                }
            }

            if (needIteration)
                Console.WriteLine($"Using active iteration: {iterationName} ({iterationPath})");
            if (needArea)
                Console.WriteLine($"Using team area: {areaPath}");

            var operations = new List<JsonPatchOperation>
            {
                new() { Op = "add", Path = "/fields/System.Title", Value = opts.Title },
                new() { Op = "add", Path = "/fields/System.IterationPath", Value = iterationPath },
                new() { Op = "add", Path = "/fields/System.AreaPath", Value = areaPath }
            };

            if (!string.IsNullOrEmpty(opts.State))
                operations.Add(new() { Op = "add", Path = "/fields/System.State", Value = opts.State });

            if (!string.IsNullOrEmpty(opts.AssignedTo))
                operations.Add(new() { Op = "add", Path = "/fields/System.AssignedTo", Value = ConfigService.ResolveAssignedTo(opts.AssignedTo) });

            if (!string.IsNullOrEmpty(opts.Description))
                operations.Add(new() { Op = "add", Path = "/fields/System.Description", Value = opts.Description });

            if (opts.Priority.HasValue)
                operations.Add(new() { Op = "add", Path = "/fields/Microsoft.VSTS.Common.Priority", Value = opts.Priority.Value });

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

            var item = await HttpService.CreateWorkItem(project, opts.Type, operations, ct);
            ConsoleHelper.WriteSuccess($"Work item #{item.Id} created: {item.Fields.Title} [{iterationPath}]");
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
