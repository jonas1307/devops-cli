namespace DevOps.Utils;

public static class GitHelper
{
    /// <summary>
    /// Returns the current git branch by reading .git/HEAD, searching from the working
    /// directory upward. Returns null when not in a git repository or in a detached HEAD.
    /// </summary>
    public static string CurrentBranch()
    {
        try
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                var head = Path.Combine(dir.FullName, ".git", "HEAD");
                if (File.Exists(head))
                {
                    var content = File.ReadAllText(head).Trim();
                    const string prefix = "ref: refs/heads/";
                    return content.StartsWith(prefix, StringComparison.Ordinal) ? content[prefix.Length..] : null;
                }
                dir = dir.Parent;
            }
        }
        catch
        {
            // best-effort only
        }

        return null;
    }
}
