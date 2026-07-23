using CommandLine;

namespace DevOps.Options;

[Verb("get", HelpText = "Get details of a work item.")]
public class GetOptions
{
    [Option('i', "id", Required = true, HelpText = "Work item ID.")]
    public int Id { get; set; }

    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output format: 'json' or 'csv'. Defaults to a detailed view.")]
    public string Output { get; set; }
}
