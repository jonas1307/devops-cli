using CommandLine;

namespace DevOps.Options;

[Verb("list", HelpText = "List work items.")]
public class ListOptions
{
    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option('s', "state", Required = false, HelpText = "Filter by state (e.g., Active, Closed, Resolved).")]
    public string State { get; set; }

    [Option('t', "type", Required = false, HelpText = "Filter by work item type (e.g., Task, Bug, User Story).")]
    public string Type { get; set; }

    [Option('a', "assigned-to", Required = false, HelpText = "Filter by assignee. Use 'me' for the current user.")]
    public string AssignedTo { get; set; }

    [Option('q', "query", Required = false, HelpText = "WIQL WHERE clause for advanced filtering.")]
    public string Query { get; set; }

    [Option("ids", Required = false, HelpText = "Show work item IDs in output.")]
    public bool ShowIds { get; set; }
}
