using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace DevOps.Services;

public record TeamCache
{
    public string IterationPath { get; set; }
    public string IterationName { get; set; }
    public string AreaPath { get; set; }
    public DateTime CachedAt { get; set; }

    public bool IsValid()
    {
        var now = DateTime.Now;
        return CachedAt.Year == now.Year && CachedAt.Month == now.Month;
    }
}

public static class CacheService
{
    private const string APPLICATION_NAME = "DevOps.Console";
    private const string CACHE_FILE_NAME = "cache.json";

    private static string GetCachePath()
    {
        var folderPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APPLICATION_NAME)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", APPLICATION_NAME);

        return Path.Combine(folderPath, CACHE_FILE_NAME);
    }

    public static TeamCache Load()
    {
        var path = GetCachePath();
        if (!File.Exists(path)) return null;
        return JsonConvert.DeserializeObject<TeamCache>(File.ReadAllText(path));
    }

    public static void Save(TeamCache cache)
    {
        var path = GetCachePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonConvert.SerializeObject(cache, Formatting.Indented));
    }

    public static void Clear()
    {
        var path = GetCachePath();
        if (File.Exists(path))
            File.Delete(path);
    }
}
