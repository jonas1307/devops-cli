using CommandLine;

namespace DevOps.Options;

[Verb("delete", HelpText = "Delete one or more work items (moves them to the recycle bin).")]
public class DeleteOptions
{
    [Option('i', "id", Required = true, HelpText = "Work item ID(s). Space-separated after the flag: -i 1 2 3.")]
    public IEnumerable<int> Ids { get; set; }

    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option("force", Required = false, HelpText = "Skip the confirmation prompt.")]
    public bool Force { get; set; }
}
