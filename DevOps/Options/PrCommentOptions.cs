using CommandLine;

namespace DevOps.Options;

[Verb("pr-comment", HelpText = "Add a comment to a pull request.")]
public class PrCommentOptions
{
    [Option('i', "id", Required = true, HelpText = "Pull request ID.")]
    public int Id { get; set; }

    [Option('m', "message", Required = true, HelpText = "Comment text.")]
    public string Message { get; set; }
}
