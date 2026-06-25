using DevOps.Options;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace DevOps.Services;

public static class AuthModes
{
    public const string Pat = "pat";
    public const string Entra = "entra";
}

public record Config
{
    public string OrgUrl { get; set; }
    public string Pat { get; set; }
    public string TenantId { get; set; }
    public string AuthMode { get; set; }
    public string DefaultProject { get; set; }
    public string DefaultTeam { get; set; }
    public string UserDisplayName { get; set; }
    public string UserEmail { get; set; }
    public bool PatEncrypted { get; set; }
}

public static class ConfigService
{
    private const string APPLICATION_NAME = "DevOps.Console";
    private const string JSON_FILE_NAME = "config.json";

    public static string GetConfigDirectory() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_NAME)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", APPLICATION_NAME);

    private static string GetConfigPath() => Path.Combine(GetConfigDirectory(), JSON_FILE_NAME);

    public static bool ConfigExists() => File.Exists(GetConfigPath());

    public static Config LoadConfig()
    {
        var configPath = GetConfigPath();

        if (!File.Exists(configPath))
            throw new FileNotFoundException($"{JSON_FILE_NAME} does not exist. Run 'config --org <url> --pat <token>' or 'config --org <url> --login' first.");

        var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));

        if (config.PatEncrypted && OperatingSystem.IsWindows() && !string.IsNullOrEmpty(config.Pat))
            config = config with { Pat = DecryptPat(config.Pat) };

        return config;
    }

    public static void SaveConfig(ConfigOptions opts, string userDisplayName = null, string userEmail = null, string authMode = null)
    {
        var configPath = GetConfigPath();
        var folderPath = Path.GetDirectoryName(configPath);

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var existing = ConfigExists() ? LoadConfig() : new Config();

        var resolvedAuthMode = authMode
            ?? (!string.IsNullOrEmpty(opts.Pat) ? AuthModes.Pat : null)
            ?? existing.AuthMode;

        var config = new Config
        {
            OrgUrl = opts.OrgUrl ?? existing.OrgUrl,
            Pat = opts.Pat ?? existing.Pat,
            TenantId = opts.Tenant ?? existing.TenantId,
            AuthMode = resolvedAuthMode,
            DefaultProject = opts.Project ?? existing.DefaultProject,
            DefaultTeam = opts.Team ?? existing.DefaultTeam,
            UserDisplayName = userDisplayName ?? existing.UserDisplayName,
            UserEmail = opts.Email ?? userEmail ?? existing.UserEmail
        };

        WriteConfig(config);
    }

    public static void SaveDefaultProject(string project)
    {
        var config = LoadConfig() with { DefaultProject = project };
        WriteConfig(config);
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

        throw new InvalidOperationException("Cannot resolve 'me': user email not found in config. Run 'config --login' or set it with 'config --email <your@email.com>'.");
    }

    public static string ResolveTeam(string project, string team = null)
    {
        if (!string.IsNullOrEmpty(team)) return team;
        var config = LoadConfig();
        if (!string.IsNullOrEmpty(config.DefaultTeam)) return config.DefaultTeam;
        return $"{project} Team";
    }

    private static void WriteConfig(Config config)
    {
        var configPath = GetConfigPath();

        if (OperatingSystem.IsWindows() && !string.IsNullOrEmpty(config.Pat))
            config = config with { Pat = EncryptPat(config.Pat), PatEncrypted = true };

        File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));

        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(configPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [SupportedOSPlatform("windows")]
    private static string EncryptPat(string pat)
    {
        var bytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(pat),
            null,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(bytes);
    }

    [SupportedOSPlatform("windows")]
    private static string DecryptPat(string encrypted)
    {
        var bytes = ProtectedData.Unprotect(
            Convert.FromBase64String(encrypted),
            null,
            DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
