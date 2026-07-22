using CommandLine;

namespace DevOps.Options;

[Verb("runs", HelpText = "List recent runs of a pipeline.")]
public class RunsOptions
{
    [Option('i', "id", Required = true, HelpText = "Pipeline (definition) ID. See 'pipelines'.")]
    public int PipelineId { get; set; }

    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option('t', "top", Required = false, Default = 10, HelpText = "Number of most recent runs to show (default: 10).")]
    public int Top { get; set; }
}
