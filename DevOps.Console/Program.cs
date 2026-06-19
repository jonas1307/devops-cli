using CommandLine;
using DevOps.Options;
using DevOps.Requests;
using DevOps.Responses;
using DevOps.Services;
using DevOps.Utils;
using DevOps.Validators;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var result = Parser.Default.ParseArguments<ConfigOptions, GetOptions, ListOptions, MineOptions, CreateOptions, UpdateOptions, StateOptions, CommentOptions, PipelinesOptions, OpenOptions>(args);

await result.MapResult(
    (ConfigOptions opts) => ConfigAction(opts, cts.Token),
    (GetOptions opts) => GetAction(opts, cts.Token),
    (ListOptions opts) => ListAction(opts, cts.Token),
    (MineOptions opts) => MineAction(opts, cts.Token),
    (CreateOptions opts) => CreateAction(opts, cts.Token),
    (UpdateOptions opts) => UpdateAction(opts, cts.Token),
    (StateOptions opts) => StateAction(opts, cts.Token),
    (CommentOptions opts) => CommentAction(opts, cts.Token),
    (PipelinesOptions opts) => PipelinesAction(opts, cts.Token),
    (OpenOptions opts) => OpenAction(opts, cts.Token),
    _ => Task.FromResult(1)
);

static async Task<int> ConfigAction(ConfigOptions opts, CancellationToken ct)
{
    var validator = new ConfigValidator();
    var validation = validator.Validate(opts);
    if (!validation.IsValid)
    {
        foreach (var error in validation.Errors)
            ConsoleHelper.WriteError(error.ErrorMessage);
        return 1;
    }

    if (opts.Reset)
    {
        ConfigService.DeleteConfig();
        CacheService.Clear();
        ConsoleHelper.WriteSuccess("Configuration removed.");
        return 0;
    }

    if (opts.RefreshCache)
    {
        CacheService.Clear();
        ConsoleHelper.WriteSuccess("Cache cleared. Iteration and area path will be re-fetched on next create.");
        return 0;
    }

    if (opts.Show)
    {
        if (!ConfigService.ConfigExists())
        {
            ConsoleHelper.WriteError("No configuration found. Run 'config --org <url> --pat <token>' first.");
            return 1;
        }

        var config = ConfigService.LoadConfig();
        var pat = config.Pat;
        var maskedPat = pat is { Length: > 4 }
            ? $"{"*".PadRight(pat.Length - 4, '*')}{pat[^4..]}"
            : "****";
        Console.WriteLine($"Organization : {config.OrgUrl}");
        Console.WriteLine($"PAT          : {maskedPat}");
        if (!string.IsNullOrEmpty(config.DefaultProject))
            Console.WriteLine($"Default Proj : {config.DefaultProject}");
        if (!string.IsNullOrEmpty(config.DefaultTeam))
            Console.WriteLine($"Default Team : {config.DefaultTeam}");
        if (!string.IsNullOrEmpty(config.UserDisplayName))
            Console.WriteLine($"Logged in as : {config.UserDisplayName} ({config.UserEmail})");
        return 0;
    }

    string userDisplayName = null;
    string userEmail = null;

    if (!string.IsNullOrEmpty(opts.OrgUrl) || !string.IsNullOrEmpty(opts.Pat))
    {
        try
        {
            var user = await HttpService.GetCurrentUser(ct);
            userDisplayName = user.DisplayName;
            userEmail = user.Properties?.Account?.Value;

            if (userEmail == null)
                ConsoleHelper.WriteError("Warning: could not detect your email automatically. Use 'config --email <your@email.com>' to set it manually so '--assigned-to me' works.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Warning: could not fetch user info ({ex.Message}). Use 'config --email <your@email.com>' to set it manually.");
        }
    }

    ConfigService.SaveConfig(opts, userDisplayName, userEmail);

    if (userDisplayName != null && userEmail != null)
        ConsoleHelper.WriteSuccess($"Configuration saved. Logged in as: {userDisplayName} ({userEmail})");
    else
        ConsoleHelper.WriteSuccess("Configuration saved.");
    return 0;
}

static async Task<int> GetAction(GetOptions opts, CancellationToken ct)
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

static async Task<int> ListAction(ListOptions opts, CancellationToken ct)
{
    try
    {
        var project = ConfigService.ResolveProject(opts.Project);
        var items = await HttpService.ListWorkItems(project, opts.State, opts.Type, opts.AssignedTo, opts.Query, ct);

        if (items.Count == 0)
        {
            Console.WriteLine("No work items found.");
            return 0;
        }

        var idWidth = items.Max(x => x.Id.ToString().Length);
        var typeWidth = Math.Min(items.Max(x => (x.Fields.WorkItemType ?? "").Length), 12);
        var stateWidth = Math.Min(items.Max(x => (x.Fields.State ?? "").Length), 12);
        var assigneeWidth = 20;
        var titleWidth = Console.WindowWidth - idWidth - typeWidth + stateWidth + assigneeWidth + 12;
        titleWidth = Math.Clamp(titleWidth, 20, 60);

        Console.WriteLine($"{"ID".PadLeft(idWidth)}  {"TYPE".PadRight(typeWidth)}  {"STATE".PadRight(stateWidth)}  {"ASSIGNED TO".PadRight(assigneeWidth)}  TITLE");
        Console.WriteLine(new string('-', idWidth + typeWidth + stateWidth + assigneeWidth + titleWidth + 10));

        foreach (var item in items)
        {
            var id = item.Id.ToString().PadLeft(idWidth);
            var type = Truncate(item.Fields.WorkItemType, typeWidth).PadRight(typeWidth);
            var state = Truncate(item.Fields.State, stateWidth).PadRight(stateWidth);
            var assignee = Truncate(item.Fields.AssignedTo?.DisplayName ?? "-", assigneeWidth).PadRight(assigneeWidth);
            var title = Truncate(item.Fields.Title, titleWidth);
            Console.WriteLine($"{id}  {type}  {state}  {assignee}  {title}");
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {items.Count} work item(s)");
        return 0;
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteError($"Error: {ex.Message}");
        return 1;
    }
}

static async Task<int> MineAction(MineOptions opts, CancellationToken ct)
{
    try
    {
        var project = ConfigService.ResolveProject(opts.Project);
        var items = await HttpService.ListWorkItems(project, opts.State, opts.Type, "me", opts.Query, ct);

        if (items.Count == 0)
        {
            Console.WriteLine("No work items assigned to you.");
            return 0;
        }

        var idWidth = items.Max(x => x.Id.ToString().Length);
        var typeWidth = Math.Min(items.Max(x => (x.Fields.WorkItemType ?? "").Length), 12);
        var stateWidth = Math.Min(items.Max(x => (x.Fields.State ?? "").Length), 12);
        var titleWidth = Console.WindowWidth - idWidth - typeWidth - stateWidth - 8;
        titleWidth = Math.Clamp(titleWidth, 20, 70);

        Console.WriteLine($"{"ID".PadLeft(idWidth)}  {"TYPE".PadRight(typeWidth)}  {"STATE".PadRight(stateWidth)}  TITLE");
        Console.WriteLine(new string('-', idWidth + typeWidth + stateWidth + titleWidth + 8));

        foreach (var item in items)
        {
            var id = item.Id.ToString().PadLeft(idWidth);
            var type = Truncate(item.Fields.WorkItemType, typeWidth).PadRight(typeWidth);
            var state = Truncate(item.Fields.State, stateWidth).PadRight(stateWidth);
            var title = Truncate(item.Fields.Title, titleWidth);
            Console.WriteLine($"{id}  {type}  {state}  {title}");
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {items.Count} work item(s)");
        return 0;
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteError($"Error: {ex.Message}");
        return 1;
    }
}

static async Task<int> CommentAction(CommentOptions opts, CancellationToken ct)
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

static async Task<int> CreateAction(CreateOptions opts, CancellationToken ct)
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
            var relType = ResolveRelationType(opts.RelationType);
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

static async Task<int> UpdateAction(UpdateOptions opts, CancellationToken ct)
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
            var relType = ResolveRelationType(opts.RelationType);
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

static async Task<int> PipelinesAction(PipelinesOptions opts, CancellationToken ct)
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
            Console.WriteLine($"{p.Id.ToString().PadLeft(idWidth)}  {Truncate(p.Name, nameWidth).PadRight(nameWidth)}  {folder}");
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

static async Task<int> StateAction(StateOptions opts, CancellationToken ct)
{
    try
    {
        var project = ConfigService.ResolveProject(opts.Project);
        var current = await HttpService.GetWorkItem(opts.Id, project, ct);
        var previousState = current.Fields.State;

        var operations = new List<JsonPatchOperation>
        {
            new() { Op = "add", Path = "/fields/System.State", Value = opts.State }
        };

        var item = await HttpService.UpdateWorkItem(opts.Id, project, operations, ct);
        ConsoleHelper.WriteSuccess($"Work item #{item.Id}: {previousState} -> {item.Fields.State}");
        return 0;
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteError($"Error: {ex.Message}");
        return 1;
    }
}

static Task<int> OpenAction(OpenOptions opts, CancellationToken ct)
{
    try
    {
        var config = ConfigService.LoadConfig();
        var project = ConfigService.ResolveProject(opts.Project);

        var orgUrl = config.OrgUrl.TrimEnd('/');
        var url = $"{orgUrl}/{Uri.EscapeDataString(project)}/_workitems/edit/{opts.Id}";

        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });

        Console.WriteLine($"Opening: {url}");
        return Task.FromResult(0);
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteError($"Error: {ex.Message}");
        return Task.FromResult(1);
    }
}

static string Truncate(string value, int max)
{
    if (string.IsNullOrEmpty(value)) return string.Empty;
    return value.Length > max ? value[..(max - 1)] + "…" : value;
}

static string ResolveRelationType(string friendly) => friendly?.ToLowerInvariant() switch
{
    "parent" => "System.LinkTypes.Hierarchy-Reverse",
    "child" => "System.LinkTypes.Hierarchy-Forward",
    "related" => "System.LinkTypes.Related",
    "blocks" => "System.LinkTypes.Dependency-Forward",
    "blocked-by" => "System.LinkTypes.Dependency-Reverse",
    _ => throw new ArgumentException($"Unknown relation type '{friendly}'. Valid values: parent, child, related, blocks, blocked-by.")
};
