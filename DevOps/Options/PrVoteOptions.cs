using CommandLine;

namespace DevOps.Options;

[Verb("pr-vote", HelpText = "Cast your vote on a pull request.")]
public class PrVoteOptions
{
    [Option('i', "id", Required = true, HelpText = "Pull request ID.")]
    public int Id { get; set; }

    [Option('v', "vote", Required = true, HelpText = "Vote: approve, approve-suggestions, reject, wait, or reset.")]
    public string Vote { get; set; }
}
