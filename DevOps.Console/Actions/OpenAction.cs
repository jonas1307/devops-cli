using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;

namespace DevOps.Actions;

internal static class OpenAction
{
    internal static Task<int> Execute(OpenOptions opts, CancellationToken ct)
    {
        try
        {
            var config = ConfigService.LoadConfig();
            var project = ConfigService.ResolveProject(opts.Project);

            var orgUrl = config.OrgUrl.TrimEnd('/');
            var url = $"{orgUrl}/{Uri.EscapeDataString(project)}/_workitems/edit/{opts.Id}";

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });

            Console.WriteLine($"Opening: {url}");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return Task.FromResult(1);
        }
    }
}
