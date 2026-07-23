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
            var tenant = ConfigService.ConfigExists() ? ConfigService.LoadConfig().TenantId : null;
            await AuthService.SignOutAsync(tenant, ct);
            ConfigService.DeleteConfig();
            CacheService.Clear();
            ConsoleHelper.WriteSuccess("Configuration removed and signed out.");
            return 0;
        }

        if (opts.RefreshCache)
        {
            CacheService.Clear();
            ConsoleHelper.WriteSuccess("Cache cleared. Iteration and area path will be re-fetched on next create.");
            return 0;
        }

        if (opts.Logout)
        {
            var tenant = ConfigService.ConfigExists() ? ConfigService.LoadConfig().TenantId : null;
            await AuthService.SignOutAsync(tenant, ct);
            ConsoleHelper.WriteSuccess("Signed out of Microsoft Entra ID.");
            return 0;
        }

        if (opts.Show)
        {
            if (!ConfigService.ConfigExists())
            {
                ConsoleHelper.WriteError("No configuration found. Run 'config --org <url> --pat <token>' or 'config --org <url> --login' first.");
                return 1;
            }

            var config = ConfigService.LoadConfig();

            Console.WriteLine($"Organization : {config.OrgUrl}");
            Console.WriteLine($"Auth mode    : {DescribeAuthMode(config.AuthMode)}");

            if (config.AuthMode == AuthModes.Entra)
            {
                var signedInAs = await AuthService.GetSignedInUsernameAsync(config.TenantId);
                Console.WriteLine($"Tenant       : {config.TenantId ?? "(home tenant)"}");
                Console.WriteLine($"Signed in    : {signedInAs ?? "no (run 'config --login')"}");
            }
            else if (config.AuthMode == AuthModes.Pat)
            {
                var pat = config.Pat;
                var maskedPat = pat is { Length: > 4 }
                    ? $"{"*".PadRight(pat.Length - 4, '*')}{pat[^4..]}"
                    : "****";
                Console.WriteLine($"PAT          : {maskedPat}");
            }

            if (!string.IsNullOrEmpty(config.DefaultProject))
                Console.WriteLine($"Default Proj : {config.DefaultProject}");
            if (!string.IsNullOrEmpty(config.DefaultTeam))
                Console.WriteLine($"Default Team : {config.DefaultTeam}");
            if (!string.IsNullOrEmpty(config.UserDisplayName))
                Console.WriteLine($"User         : {config.UserDisplayName} ({config.UserEmail})");
            Console.WriteLine($"Table border : {config.TableBorder ?? "minimal"}");
            return 0;
        }

        if (opts.Login)
            return await SignInFlow(opts, ct);

        // PAT or plain settings (org/project/team/email) flow.
        string patUserDisplayName = null;
        string patUserEmail = null;
        string patUserId = null;

        if (!string.IsNullOrEmpty(opts.Pat))
        {
            // Persist the PAT first so the user lookup authenticates with it.
            ConfigService.SaveConfig(opts, authMode: AuthModes.Pat);
            try
            {
                var user = await HttpService.GetCurrentUser(ct);
                patUserDisplayName = user.DisplayName;
                patUserEmail = user.Properties?.Account?.Value;
                patUserId = user.Id;

                if (patUserEmail == null)
                    ConsoleHelper.WriteError("Warning: could not detect your email automatically. Use 'config --email <your@email.com>' so '--assigned-to me' works.");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Warning: could not fetch user info ({ex.Message}). Use 'config --email <your@email.com>' to set it manually.");
            }
        }

        ConfigService.SaveConfig(opts, patUserDisplayName, patUserEmail, userId: patUserId);

        if (patUserDisplayName != null && patUserEmail != null)
            ConsoleHelper.WriteSuccess($"Configuration saved. Logged in as: {patUserDisplayName} ({patUserEmail})");
        else
            ConsoleHelper.WriteSuccess("Configuration saved.");
        return 0;
    }

    private static async Task<int> SignInFlow(ConfigOptions opts, CancellationToken ct)
    {
        // Persist org/tenant/project/team/email and switch to Entra mode before signing in.
        ConfigService.SaveConfig(opts, authMode: AuthModes.Entra);

        try
        {
            var tenant = ConfigService.LoadConfig().TenantId;
            var result = await AuthService.SignInAsync(tenant, ct);

            string userDisplayName = null;
            string userEmail = null;
            string userId = null;
            try
            {
                var user = await HttpService.GetCurrentUser(ct);
                userDisplayName = user.DisplayName;
                userEmail = user.Properties?.Account?.Value;
                userId = user.Id;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Warning: signed in, but could not fetch user info ({ex.Message}). Ensure --org is set.");
            }

            userEmail ??= result.Account?.Username;
            ConfigService.SaveConfig(opts, userDisplayName, userEmail, AuthModes.Entra, userId);

            ConsoleHelper.WriteSuccess($"Signed in as {userDisplayName ?? result.Account?.Username} ({userEmail}).");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Sign-in failed: {ex.Message}");
            return 1;
        }
    }

    private static string DescribeAuthMode(string authMode) => authMode switch
    {
        AuthModes.Entra => "Microsoft Entra ID",
        AuthModes.Pat => "Personal Access Token",
        _ => "not configured"
    };
}
