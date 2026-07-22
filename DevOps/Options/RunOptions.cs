using CommandLine;

namespace DevOps.Options;

[Verb("run", HelpText = "Queue a new run of a pipeline.")]
public class RunOptions
{
    [Option('i', "id", Required = true, HelpText = "Pipeline (definition) ID. See 'pipelines'.")]
    public int PipelineId { get; set; }

    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option('b', "branch", Required = false, HelpText = "Branch to run (e.g., 'main' or 'refs/heads/main'). Defaults to the pipeline's default branch.")]
    public string Branch { get; set; }
}
