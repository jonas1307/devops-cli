using CommandLine;

namespace DevOps.Options;

[Verb("config", HelpText = "Configure the connection to Azure DevOps.")]
public class ConfigOptions
{
    [Option('o', "org", Required = false, HelpText = "Azure DevOps organization URL (e.g., https://dev.azure.com/myorg).")]
    public string OrgUrl { get; set; }

    [Option('p', "pat", Required = false, HelpText = "Personal Access Token for authentication.")]
    public string Pat { get; set; }

    [Option('l', "login", Required = false, HelpText = "Sign in interactively with Microsoft Entra ID (opens a browser).")]
    public bool Login { get; set; }

    [Option("logout", Required = false, HelpText = "Sign out and clear the cached Entra ID token.")]
    public bool Logout { get; set; }

    [Option("tenant", Required = false, HelpText = "Entra ID tenant ID or domain to sign in against (defaults to your home tenant).")]
    public string Tenant { get; set; }

    [Option('P', "project", Required = false, HelpText = "Default project name used when --project is omitted from other commands.")]
    public string Project { get; set; }

    [Option('T', "team", Required = false, HelpText = "Default team name used to resolve the active iteration (defaults to '{Project} Team').")]
    public string Team { get; set; }

    [Option("show", Required = false, HelpText = "Display the current configuration (PAT masked).")]
    public bool Show { get; set; }

    [Option("reset", Required = false, HelpText = "Remove all local configuration.")]
    public bool Reset { get; set; }

    [Option("refresh-cache", Required = false, HelpText = "Force re-fetch of iteration and area path on next create.")]
    public bool RefreshCache { get; set; }

    [Option('e', "email", Required = false, HelpText = "Your email address, used to resolve '--assigned-to me'. Set manually if auto-detection fails.")]
    public string Email { get; set; }

    [Option("border", Required = false, HelpText = "Table border style for list output: minimal (default), square, or markdown.")]
    public string Border { get; set; }
}
