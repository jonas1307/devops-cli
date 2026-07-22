using CommandLine;

namespace DevOps.Options;

[Verb("update", HelpText = "Update an existing work item.")]
public class UpdateOptions
{
    [Option('i', "id", Required = true, HelpText = "Work item ID.")]
    public int Id { get; set; }

    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option('t', "title", Required = false, HelpText = "New title.")]
    public string Title { get; set; }

    [Option('s', "state", Required = false, HelpText = "New state (e.g., Active, Closed, Resolved).")]
    public string State { get; set; }

    [Option('a', "assigned-to", Required = false, HelpText = "New assignee email or display name.")]
    public string AssignedTo { get; set; }

    [Option('d', "description", Required = false, HelpText = "New description.")]
    public string Description { get; set; }

    [Option('P', "priority", Required = false, HelpText = "New priority (1–4).")]
    public int? Priority { get; set; }

    [Option('c', "comment", Required = false, HelpText = "Add a comment to the work item.")]
    public string Comment { get; set; }

    [Option('I', "iteration", Required = false, HelpText = "New iteration path (e.g., 'MyProject\\Sprint 3').")]
    public string Iteration { get; set; }

    [Option('A', "area", Required = false, HelpText = "New area path (e.g., 'MyProject\\Backend').")]
    public string Area { get; set; }

    [Option('e', "estimate", Required = false, HelpText = "Estimated work in hours (Custom.EstimateWork).")]
    public decimal? Estimate { get; set; }

    [Option('y', "activity-type", Required = false, HelpText = "Activity type (e.g., Development, Testing, Design).")]
    public string ActivityType { get; set; }

    [Option('f', "field", Required = false, HelpText = "Custom field in Key=Value format. Repeatable: -f Key1=Value1 -f Key2=Value2.")]
    public IEnumerable<string> Fields { get; set; }

    [Option('R', "related-id", Required = false, HelpText = "ID of the work item to relate to.")]
    public int? RelatedId { get; set; }

    [Option('r', "relation-type", Required = false, Default = "related", HelpText = "Relation type: parent, child, related, blocks, blocked-by (default: related).")]
    public string RelationType { get; set; }
}
