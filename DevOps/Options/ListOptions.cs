using CommandLine;

namespace DevOps.Options;

[Verb("list", HelpText = "List work items.")]
public class ListOptions
{
    [Option('P', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option('s', "state", Required = false, HelpText = "Filter by state (e.g., Active, Closed, Resolved).")]
    public string State { get; set; }

    [Option('t', "type", Required = false, HelpText = "Filter by work item type (e.g., Task, Bug, User Story).")]
    public string Type { get; set; }

    [Option('a', "assigned-to", Required = false, HelpText = "Filter by assignee. Use 'me' for the current user.")]
    public string AssignedTo { get; set; }

    [Option('q', "query", Required = false, HelpText = "WIQL WHERE clause for advanced filtering.")]
    public string Query { get; set; }

    [Option('p', "parent", Required = false, HelpText = "Filter by parent work item ID.")]
    public int? ParentId { get; set; }

    [Option("ids", Required = false, HelpText = "Show work item IDs in output.")]
    public bool ShowIds { get; set; }

    [Option('n', "top", Required = false, Default = 50, HelpText = "Maximum number of work items to fetch (default: 50).")]
    public int Top { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output format: 'json' or 'csv'. Defaults to a table.")]
    public string Output { get; set; }
}
