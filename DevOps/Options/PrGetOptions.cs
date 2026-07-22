using CommandLine;

namespace DevOps.Options;

[Verb("pr-get", HelpText = "Show pull request details.")]
public class PrGetOptions
{
    [Option('i', "id", Required = true, HelpText = "Pull request ID.")]
    public int Id { get; set; }
}
