using CommandLine;

namespace DevOps.Options;

[Verb("pr-open", HelpText = "Open a pull request in the browser.")]
public class PrOpenOptions
{
    [Option('i', "id", Required = true, HelpText = "Pull request ID.")]
    public int Id { get; set; }
}
