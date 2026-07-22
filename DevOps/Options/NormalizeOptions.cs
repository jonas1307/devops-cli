using CommandLine;

namespace DevOps.Options;

[Verb("normalize", HelpText = "Normalize work item titles to include the parent type and ID prefix.")]
public class NormalizeOptions
{
    [Option('P', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option('s', "state", Required = false, HelpText = "Filter by state (e.g., Active, New).")]
    public string State { get; set; }

    [Option('a', "assigned-to", Required = false, Default = "me", HelpText = "Filter by assignee. Use 'me' for the current user, or 'any' for all. Defaults to 'me'.")]
    public string AssignedTo { get; set; }

    [Option('p', "parent", Required = false, HelpText = "Restrict to children of a specific parent ID.")]
    public int? ParentId { get; set; }

    [Option('n', "dry-run", Required = false, HelpText = "Preview changes without applying them.")]
    public bool DryRun { get; set; }
}
