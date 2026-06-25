using CommandLine;

namespace DevOps.Options;

[Verb("create", HelpText = "Create a new work item.")]
public class CreateOptions
{
    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option("type", Required = false, Default = "Task", HelpText = "Work item type (e.g., Task, Bug, User Story).")]
    public string Type { get; set; }

    [Option('t', "title", Required = true, HelpText = "Title of the work item.")]
    public string Title { get; set; }

    [Option('s', "state", Required = false, HelpText = "Initial state (e.g., Active, New).")]
    public string State { get; set; }

    [Option('a', "assigned-to", Required = false, HelpText = "Assignee email or display name.")]
    public string AssignedTo { get; set; }

    [Option('d', "description", Required = false, HelpText = "Description of the work item.")]
    public string Description { get; set; }

    [Option('P', "priority", Required = false, HelpText = "Priority (1–4).")]
    public int? Priority { get; set; }

    [Option('I', "iteration", Required = false, HelpText = "Iteration path (e.g., 'MyProject\\Sprint 3'). Defaults to the active iteration.")]
    public string Iteration { get; set; }

    [Option('A', "area", Required = false, HelpText = "Area path (e.g., 'MyProject\\Backend'). Defaults to the team's default area.")]
    public string Area { get; set; }

    [Option('R', "related-id", Required = false, HelpText = "ID of the work item to relate to.")]
    public int? RelatedId { get; set; }

    [Option('r', "relation-type", Required = false, Default = "parent", HelpText = "Relation type: parent, child, related, blocks, blocked-by (default: parent).")]
    public string RelationType { get; set; }

    [Option('e', "estimate", Required = false, HelpText = "Estimated work in hours (Custom.EstimateWork).")]
    public decimal? Estimate { get; set; }

    [Option('y', "activity-type", Required = false, HelpText = "Activity type (e.g., Development, Testing, Design).")]
    public string ActivityType { get; set; }

    [Option('f', "field", Required = false, HelpText = "Custom field in Key=Value format. Repeatable: -f Key1=Value1 -f Key2=Value2.")]
    public IEnumerable<string> Fields { get; set; }

    [Option("normalize", Required = false, HelpText = "Prefix the title with the parent type and ID (e.g., 'PBI 12345 - <title>'). Requires a parent relation.")]
    public bool Normalize { get; set; }
}
