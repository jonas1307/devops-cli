using CommandLine;

namespace DevOps.Options;

[Verb("comment", HelpText = "Add a comment to a work item.")]
public class CommentOptions
{
    [Option('i', "id", Required = true, HelpText = "Work item ID.")]
    public int Id { get; set; }

    [Value(0, Required = true, MetaName = "message", HelpText = "Comment text.")]
    public string Message { get; set; }

    [Option('p', "project", Required = false, HelpText = "Project name. Uses default if configured.")]
    public string Project { get; set; }
}
