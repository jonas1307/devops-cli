using CommandLine;

namespace DevOps.Options;

[Verb("pr-list", HelpText = "List pull requests.")]
public class PrListOptions
{
    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option('r', "repo", Required = false, HelpText = "Repository name. If omitted, lists across all repos in the project.")]
    public string Repo { get; set; }

    [Option('s', "status", Required = false, Default = "active", HelpText = "Filter by status: active, completed, abandoned, all.")]
    public string Status { get; set; }

    [Option('t', "target", Required = false, HelpText = "Filter by target branch (e.g., main).")]
    public string Target { get; set; }

    [Option('n', "top", Required = false, Default = 25, HelpText = "Maximum number of pull requests to show (default: 25).")]
    public int Top { get; set; }
}
