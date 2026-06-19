using CommandLine;

namespace DevOps.Options;

[Verb("open", HelpText = "Open a work item in the browser.")]
public class OpenOptions
{
    [Option('i', "id", Required = true, HelpText = "Work item ID.")]
    public int Id { get; set; }

    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }
}
