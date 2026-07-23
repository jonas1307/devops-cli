using CommandLine;

namespace DevOps.Options;

[Verb("pr-abandon", HelpText = "Abandon a pull request.")]
public class PrAbandonOptions
{
    [Option('i', "id", Required = true, HelpText = "Pull request ID.")]
    public int Id { get; set; }
}
