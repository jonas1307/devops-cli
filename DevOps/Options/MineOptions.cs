using CommandLine;

namespace DevOps.Options;

[Verb("mine", HelpText = "List work items assigned to me.")]
public class MineOptions
{
    [Option('P', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option('s', "state", Required = false, HelpText = "Filter by state (e.g., Active, Closed, Resolved).")]
    public string State { get; set; }

    [Option('t', "type", Required = false, HelpText = "Filter by work item type (e.g., Task, Bug, User Story).")]
    public string Type { get; set; }

    [Option('q', "query", Required = false, HelpText = "Additional WIQL WHERE clause.")]
    public string Query { get; set; }

    [Option('p', "parent", Required = false, HelpText = "Filter by parent work item ID.")]
    public int? ParentId { get; set; }

    [Option('n', "top", Required = false, Default = 50, HelpText = "Maximum number of work items to fetch (default: 50).")]
    public int Top { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output format: 'json' or 'csv'. Defaults to a table.")]
    public string Output { get; set; }
}
