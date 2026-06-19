using CommandLine;

namespace DevOps.Options;

[Verb("pipelines", HelpText = "List available pipelines.")]
public class PipelinesOptions
{
    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option('n', "name", Required = false, HelpText = "Filter by pipeline name (partial match).")]
    public string Name { get; set; }
}
