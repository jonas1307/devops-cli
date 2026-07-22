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
}
