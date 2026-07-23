using CommandLine;
using DevOps.Actions;
using DevOps.Options;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// The generic ParseArguments<...>/MapResult overloads cap at 16 verb types;
// with more than that we parse against an explicit type array and dispatch by type.
var optionTypes = new[]
{
    typeof(ConfigOptions),
    typeof(GetOptions),
    typeof(ListOptions),
    typeof(MineOptions),
    typeof(CreateOptions),
    typeof(UpdateOptions),
    typeof(StateOptions),
    typeof(DeleteOptions),
    typeof(CommentOptions),
    typeof(PipelinesOptions),
    typeof(RunsOptions),
    typeof(RunOptions),
    typeof(OpenOptions),
    typeof(NormalizeOptions),
    typeof(PrListOptions),
    typeof(PrGetOptions),
    typeof(PrCreateOptions),
    typeof(PrOpenOptions),
    typeof(PrVoteOptions),
    typeof(PrAbandonOptions),
    typeof(PrCompleteOptions)
};

var result = Parser.Default.ParseArguments(args, optionTypes);

await result.MapResult(
    (object opts) => opts switch
    {
        ConfigOptions o => ConfigAction.Execute(o, cts.Token),
        GetOptions o => GetAction.Execute(o, cts.Token),
        ListOptions o => ListAction.Execute(o, cts.Token),
        MineOptions o => MineAction.Execute(o, cts.Token),
        CreateOptions o => CreateAction.Execute(o, cts.Token),
        UpdateOptions o => UpdateAction.Execute(o, cts.Token),
        StateOptions o => StateAction.Execute(o, cts.Token),
        DeleteOptions o => DeleteAction.Execute(o, cts.Token),
        CommentOptions o => CommentAction.Execute(o, cts.Token),
        PipelinesOptions o => PipelinesAction.Execute(o, cts.Token),
        RunsOptions o => RunsAction.Execute(o, cts.Token),
        RunOptions o => RunAction.Execute(o, cts.Token),
        OpenOptions o => OpenAction.Execute(o, cts.Token),
        NormalizeOptions o => NormalizeAction.Execute(o, cts.Token),
        PrListOptions o => PrListAction.Execute(o, cts.Token),
        PrGetOptions o => PrGetAction.Execute(o, cts.Token),
        PrCreateOptions o => PrCreateAction.Execute(o, cts.Token),
        PrOpenOptions o => PrOpenAction.Execute(o, cts.Token),
        PrVoteOptions o => PrVoteAction.Execute(o, cts.Token),
        PrAbandonOptions o => PrAbandonAction.Execute(o, cts.Token),
        PrCompleteOptions o => PrCompleteAction.Execute(o, cts.Token),
        _ => Task.FromResult(1)
    },
    _ => Task.FromResult(1)
);
