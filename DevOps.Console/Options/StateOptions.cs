using CommandLine;

namespace DevOps.Options;

[Verb("state", HelpText = "Change the state of a work item.")]
public class StateOptions
{
    [Option('i', "id", Required = true, HelpText = "Work item ID(s). Repeatable: -i 1 -i 2.")]
    public IEnumerable<int> Ids { get; set; }

    [Option('s', "state", Required = true, HelpText = "Target state (e.g., \"In Progress\", \"Closed\", \"Done\").")]
    public string State { get; set; }

    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }
}
