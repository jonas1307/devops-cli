using DevOps.Options;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace DevOps.Services;

public record Config
{
    public string OrgUrl { get; set; }
    public string Pat { get; set; }
    public string DefaultProject { get; set; }
    public string DefaultTeam { get; set; }
    public string UserDisplayName { get; set; }
    public string UserEmail { get; set; }
}

public static class ConfigService
{
    private const string APPLICATION_NAME = "DevOps.Console";
    private const string JSON_FILE_NAME = "config.json";

    private static string GetConfigPath()
    {
        var folderPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_NAME)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", APPLICATION_NAME);

        return Path.Combine(folderPath, JSON_FILE_NAME);
    }

    public static bool ConfigExists() => File.Exists(GetConfigPath());

    public static Config LoadConfig()
    {
        var configPath = GetConfigPath();

        if (!File.Exists(configPath))
            throw new FileNotFoundException($"{JSON_FILE_NAME} does not exist. Run 'config --org <url> --pat <token>' first.");

        return JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
    }

    public static void SaveConfig(ConfigOptions opts, string userDisplayName = null, string userEmail = null)
    {
        var configPath = GetConfigPath();
        var folderPath = Path.GetDirectoryName(configPath);

        var existing = ConfigExists() ? LoadConfig() : new Config();

        var config = new Config
        {
            OrgUrl = opts.OrgUrl ?? existing.OrgUrl,
            Pat = opts.Pat ?? existing.Pat,
            DefaultProject = opts.Project ?? existing.DefaultProject,
            DefaultTeam = opts.Team ?? existing.DefaultTeam,
            UserDisplayName = userDisplayName ?? existing.UserDisplayName,
            UserEmail = opts.Email ?? userEmail ?? existing.UserEmail
        };

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }

    public static void SaveDefaultProject(string project)
    {
        var config = LoadConfig();
        var updated = config with { DefaultProject = project };
        File.WriteAllText(GetConfigPath(), JsonConvert.SerializeObject(updated, Formatting.Indented));
    }

    public static void DeleteConfig()
    {
        var configPath = GetConfigPath();
        if (File.Exists(configPath))
            File.Delete(configPath);
    }

    public static string ResolveProject(string project)
    {
        if (!string.IsNullOrEmpty(project)) return project;
        var config = LoadConfig();
        if (!string.IsNullOrEmpty(config.DefaultProject)) return config.DefaultProject;
        throw new InvalidOperationException("No project specified. Use --project or set a default with 'config --project <name>'.");
    }

    public static string ResolveAssignedTo(string assignedTo)
    {
        if (!assignedTo.Equals("me", StringComparison.OrdinalIgnoreCase))
            return assignedTo;

        var config = LoadConfig();
        if (!string.IsNullOrEmpty(config.UserEmail))
            return config.UserEmail;

        throw new InvalidOperationException("Cannot resolve 'me': user email not found in config. Run 'config --org <url> --pat <token>' to refresh user info.");
    }

    public static string ResolveTeam(string project, string team = null)
    {
        if (!string.IsNullOrEmpty(team)) return team;
        var config = LoadConfig();
        if (!string.IsNullOrEmpty(config.DefaultTeam)) return config.DefaultTeam;
        return $"{project} Team";
    }
}
