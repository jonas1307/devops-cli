using DevOps.Options;
using DevOps.Services;
using DevOps.Utils;
using DevOps.Validators;

namespace DevOps.Actions;

internal static class ConfigAction
{
    internal static async Task<int> Execute(ConfigOptions opts, CancellationToken ct)
    {
        var validator = new ConfigValidator();
        var validation = validator.Validate(opts);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ConsoleHelper.WriteError(error.ErrorMessage);
            return 1;
        }

        if (opts.Reset)
        {
            ConfigService.DeleteConfig();
            CacheService.Clear();
            ConsoleHelper.WriteSuccess("Configuration removed.");
            return 0;
        }

        if (opts.RefreshCache)
        {
            CacheService.Clear();
            ConsoleHelper.WriteSuccess("Cache cleared. Iteration and area path will be re-fetched on next create.");
            return 0;
        }

        if (opts.Show)
        {
            if (!ConfigService.ConfigExists())
            {
                ConsoleHelper.WriteError("No configuration found. Run 'config --org <url> --pat <token>' first.");
                return 1;
            }

            var config = ConfigService.LoadConfig();
            var pat = config.Pat;
            var maskedPat = pat is { Length: > 4 }
                ? $"{"*".PadRight(pat.Length - 4, '*')}{pat[^4..]}"
                : "****";
            Console.WriteLine($"Organization : {config.OrgUrl}");
            Console.WriteLine($"PAT          : {maskedPat}");
            if (!string.IsNullOrEmpty(config.DefaultProject))
                Console.WriteLine($"Default Proj : {config.DefaultProject}");
            if (!string.IsNullOrEmpty(config.DefaultTeam))
                Console.WriteLine($"Default Team : {config.DefaultTeam}");
            if (!string.IsNullOrEmpty(config.UserDisplayName))
                Console.WriteLine($"Logged in as : {config.UserDisplayName} ({config.UserEmail})");
            return 0;
        }

        string userDisplayName = null;
        string userEmail = null;

        if (!string.IsNullOrEmpty(opts.OrgUrl) || !string.IsNullOrEmpty(opts.Pat))
        {
            try
            {
                var user = await HttpService.GetCurrentUser(ct);
                userDisplayName = user.DisplayName;
                userEmail = user.Properties?.Account?.Value;

                if (userEmail == null)
                    ConsoleHelper.WriteError("Warning: could not detect your email automatically. Use 'config --email <your@email.com>' to set it manually so '--assigned-to me' works.");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Warning: could not fetch user info ({ex.Message}). Use 'config --email <your@email.com>' to set it manually.");
            }
        }

        ConfigService.SaveConfig(opts, userDisplayName, userEmail);

        if (userDisplayName != null && userEmail != null)
            ConsoleHelper.WriteSuccess($"Configuration saved. Logged in as: {userDisplayName} ({userEmail})");
        else
            ConsoleHelper.WriteSuccess("Configuration saved.");
        return 0;
    }
}
