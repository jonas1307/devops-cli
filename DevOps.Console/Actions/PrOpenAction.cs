using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class PrOpenAction
{
    internal static async Task<int> Execute(PrOpenOptions opts, CancellationToken ct)
    {
        try
        {
            var pr = await HttpService.GetPullRequest(opts.Id, ct);
            var url = pr.Links?.Web?.Href;

            if (string.IsNullOrEmpty(url))
            {
                ConsoleHelper.WriteError($"Could not resolve a web URL for pull request {opts.Id}.");
                return 1;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });

            Console.WriteLine($"Opening: {url}");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
