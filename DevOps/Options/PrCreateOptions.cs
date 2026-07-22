using CommandLine;

namespace DevOps.Options;

[Verb("pr-create", HelpText = "Create a pull request.")]
public class PrCreateOptions
{
    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option('r', "repo", Required = true, HelpText = "Repository name.")]
    public string Repo { get; set; }

    [Option('s', "source", Required = true, HelpText = "Source branch (e.g., feature/x or refs/heads/feature/x).")]
    public string Source { get; set; }

    [Option('t', "target", Required = true, HelpText = "Target branch (e.g., main).")]
    public string Target { get; set; }

    [Option("title", Required = true, HelpText = "Pull request title.")]
    public string Title { get; set; }

    [Option('d', "description", Required = false, HelpText = "Pull request description.")]
    public string Description { get; set; }

    [Option("draft", Required = false, HelpText = "Create the pull request as a draft.")]
    public bool Draft { get; set; }
}
