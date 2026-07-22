using DevOps.Responses;
using DevOps.Services;
using Spectre.Console;

namespace DevOps.Actions;

internal static class ActionHelpers
{
    /// <summary>Creates a table with bold headers, sized to the terminal, using the configured border.</summary>
    internal static Table NewTable(params string[] columns)
    {
        var table = new Table().Border(ResolveBorder());
        foreach (var column in columns)
            table.AddColumn(new TableColumn($"[bold]{Markup.Escape(column)}[/]") { NoWrap = true });
        return table;
    }

    private static TableBorder ResolveBorder()
    {
        string configured = null;
        try
        {
            if (ConfigService.ConfigExists())
                configured = ConfigService.LoadConfig().TableBorder;
        }
        catch { /* fall back to default */ }

        return configured?.ToLowerInvariant() switch
        {
            "square" => TableBorder.Square,
            "markdown" => TableBorder.Markdown,
            _ => TableBorder.Minimal
        };
    }

    /// <summary>Colors a status/state-like value (work item state, PR status, pipeline state).</summary>
    internal static string ColorState(string state) => state?.ToLowerInvariant() switch
    {
        "active" or "inprogress" or "in progress" or "new" => $"[green]{Markup.Escape(state)}[/]",
        "completed" or "closed" or "done" or "resolved" => $"[grey]{Markup.Escape(state)}[/]",
        "abandoned" or "removed" => $"[red]{Markup.Escape(state)}[/]",
        _ => Markup.Escape(string.IsNullOrEmpty(state) ? "-" : state)
    };

    /// <summary>Colors a pipeline run result.</summary>
    internal static string ColorResult(string result) => result?.ToLowerInvariant() switch
    {
        "succeeded" => $"[green]{Markup.Escape(result)}[/]",
        "failed" => $"[red]{Markup.Escape(result)}[/]",
        "canceled" or "cancelled" => $"[yellow]{Markup.Escape(result)}[/]",
        _ => Markup.Escape(string.IsNullOrEmpty(result) ? "-" : result)
    };

    /// <summary>
    /// Prints a muted line for context around a table (totals and the like), so the
    /// table itself stands out as the actual result.
    /// </summary>
    internal static void WriteMuted(string text) => AnsiConsole.MarkupLine($"[grey]{Markup.Escape(text)}[/]");

    /// <summary>
    /// Describes how many items are shown, making truncation explicit when the query
    /// matched more than were fetched.
    /// </summary>
    internal static string DescribeCount(int shown, int totalMatched, string noun)
    {
        var plural = shown == 1 ? noun : $"{noun}s";

        return totalMatched > shown
            ? $"Showing {shown} of {totalMatched} {noun}s - use --top to fetch more."
            : $"Total: {shown} {plural}";
    }

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
