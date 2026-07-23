using CommandLine;

namespace DevOps.Options;

[Verb("pr-create", HelpText = "Create a pull request.")]
public class PrCreateOptions
{
    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }

    [Option('r', "repo", Required = true, HelpText = "Repository name.")]
    public string Repo { get; set; }

    [Option('s', "source", Required = false, HelpText = "Source branch (e.g., feature/x). Defaults to the current git branch.")]
    public string Source { get; set; }

    [Option('t', "target", Required = true, HelpText = "Target branch (e.g., main).")]
    public string Target { get; set; }

    [Option("title", Required = true, HelpText = "Pull request title.")]
    public string Title { get; set; }

    [Option('d', "description", Required = false, HelpText = "Pull request description.")]
    public string Description { get; set; }

    [Option("draft", Required = false, HelpText = "Create the pull request as a draft.")]
    public bool Draft { get; set; }

    [Option("reviewers", Required = false, Separator = ',', HelpText = "Reviewers to add: 'me', a GUID, or an email/display name (comma-separated).")]
    public IEnumerable<string> Reviewers { get; set; }

    [Option('w', "work-item", Required = false, Separator = ',', HelpText = "Work item IDs to link to the pull request (comma-separated).")]
    public IEnumerable<int> WorkItems { get; set; }
}
