using DevOps.Responses;
using DevOps.Services;

namespace DevOps.Actions;

internal static class ActionHelpers
{
    /// <summary>
    /// Resolves the browser URL for a pull request. The Azure DevOps PR payload
    /// often omits _links.web, so we fall back to building it from org/project/repo.
    /// </summary>
    internal static string ResolvePullRequestUrl(PullRequestResponse pr)
    {
        var url = pr.Links?.Web?.Href;
        if (!string.IsNullOrEmpty(url))
            return url;

        var repo = pr.Repository?.Name;
        var project = pr.Repository?.Project?.Name;
        if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(project))
            return null;

        var orgUrl = ConfigService.LoadConfig().OrgUrl.TrimEnd('/');
        return $"{orgUrl}/{Uri.EscapeDataString(project)}/_git/{Uri.EscapeDataString(repo)}/pullrequest/{pr.PullRequestId}";
    }


    internal static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length > max ? value[..(max - 3)] + "..." : value;
    }

    internal static string ShortBranch(string refName) =>
        string.IsNullOrEmpty(refName) ? "-" : refName.Replace("refs/heads/", "");

    internal static string VoteText(int vote) => vote switch
    {
        10 => "approved",
        5 => "approved w/ suggestions",
        0 => "no vote",
        -5 => "waiting",
        -10 => "rejected",
        _ => vote.ToString()
    };

    internal static string ParentTypeAbbreviation(string workItemType) =>
        workItemType == "Product Backlog Item"
            ? "PBI"
            : workItemType?.ToUpperInvariant() ?? "UNKNOWN";

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
