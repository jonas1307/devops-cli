using CommandLine;

namespace DevOps.Options;

[Verb("pr-complete", HelpText = "Complete (merge) a pull request.")]
public class PrCompleteOptions
{
    [Option('i', "id", Required = true, HelpText = "Pull request ID.")]
    public int Id { get; set; }

    [Option("delete-source", Required = false, HelpText = "Delete the source branch after completing.")]
    public bool DeleteSource { get; set; }
}
