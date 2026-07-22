using CommandLine;
using DevOps.Actions;
using DevOps.Options;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var result = Parser.Default.ParseArguments<
    ConfigOptions,
    GetOptions,
    ListOptions,
    MineOptions,
    CreateOptions,
    UpdateOptions,
    StateOptions,
    CommentOptions,
    PipelinesOptions,
    RunsOptions,
    RunOptions,
    OpenOptions,
    NormalizeOptions>(args);

await result.MapResult(
    (ConfigOptions opts) => ConfigAction.Execute(opts, cts.Token),
    (GetOptions opts) => GetAction.Execute(opts, cts.Token),
    (ListOptions opts) => ListAction.Execute(opts, cts.Token),
    (MineOptions opts) => MineAction.Execute(opts, cts.Token),
    (CreateOptions opts) => CreateAction.Execute(opts, cts.Token),
    (UpdateOptions opts) => UpdateAction.Execute(opts, cts.Token),
    (StateOptions opts) => StateAction.Execute(opts, cts.Token),
    (CommentOptions opts) => CommentAction.Execute(opts, cts.Token),
    (PipelinesOptions opts) => PipelinesAction.Execute(opts, cts.Token),
    (RunsOptions opts) => RunsAction.Execute(opts, cts.Token),
    (RunOptions opts) => RunAction.Execute(opts, cts.Token),
    (OpenOptions opts) => OpenAction.Execute(opts, cts.Token),
    (NormalizeOptions opts) => NormalizeAction.Execute(opts, cts.Token),
    _ => Task.FromResult(1)
);
