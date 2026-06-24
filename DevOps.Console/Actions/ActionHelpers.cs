namespace DevOps.Actions;

internal static class ActionHelpers
{
    internal static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length > max ? value[..(max - 1)] + "…" : value;
    }

    internal static string ResolveRelationType(string friendly) => friendly?.ToLowerInvariant() switch
    {
        "parent" => "System.LinkTypes.Hierarchy-Reverse",
        "child" => "System.LinkTypes.Hierarchy-Forward",
        "related" => "System.LinkTypes.Related",
        "blocks" => "System.LinkTypes.Dependency-Forward",
        "blocked-by" => "System.LinkTypes.Dependency-Reverse",
        _ => throw new ArgumentException($"Unknown relation type '{friendly}'. Valid values: parent, child, related, blocks, blocked-by.")
    };
}
