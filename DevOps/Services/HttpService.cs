using DevOps.Requests;
using DevOps.Responses;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace DevOps.Services;

public static class HttpService
{
    private const string API_VERSION = "7.1";

    // The work item batch endpoint accepts at most 200 ids per request.
    private const int WorkItemBatchSize = 200;

    private const string WorkItemFields =
        "System.Id,System.Title,System.State,System.WorkItemType,System.AssignedTo," +
        "System.Description,Microsoft.VSTS.Common.Priority,System.CreatedDate," +
        "System.ChangedDate,System.TeamProject,System.Parent";

    private static async Task<RestClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        var config = ConfigService.LoadConfig();

        IAuthenticator authenticator;
        if (config.AuthMode == AuthModes.Entra)
        {
            var token = await AuthService.GetAccessTokenAsync(config.TenantId, cancellationToken);
            authenticator = new JwtAuthenticator(token);
        }
        else if (!string.IsNullOrEmpty(config.Pat))
        {
            authenticator = new HttpBasicAuthenticator(string.Empty, config.Pat);
        }
        else
        {
            throw new InvalidOperationException("No authentication configured. Run 'config --pat <token>' or 'config --login'.");
        }

        var options = new RestClientOptions(config.OrgUrl) { Authenticator = authenticator };
        return new RestClient(options);
    }

    public static async Task<WorkItemResponse> GetWorkItem(int id, string project, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/_apis/wit/workitems/{id}", Method.Get);
        request.AddQueryParameter("api-version", API_VERSION);
        request.AddQueryParameter("$expand", "all");

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to get work item {id}. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<WorkItemResponse>(response.Content);
    }

    /// <summary>
    /// Runs a WIQL query and fetches the matching work items, returning the requested
    /// page along with how many items matched in total (so callers can report truncation).
    /// </summary>
    public static async Task<(List<WorkItemResponse> Items, int TotalMatched)> ListWorkItems(string project, string state, string type, string assignedTo, string customQuery, int? parentId = null, int top = 50, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);

        var conditions = new List<string>
        {
            $"[System.TeamProject] = '{project}'"
        };

        if (!string.IsNullOrEmpty(state))
            conditions.Add($"[System.State] = '{state}'");

        if (!string.IsNullOrEmpty(type))
            conditions.Add($"[System.WorkItemType] = '{type}'");

        if (!string.IsNullOrEmpty(assignedTo))
        {
            var assignee = assignedTo.Equals("me", StringComparison.OrdinalIgnoreCase) ? "@Me" : $"'{assignedTo}'";
            conditions.Add($"[System.AssignedTo] = {assignee}");
        }

        if (!string.IsNullOrEmpty(customQuery))
            conditions.Add(customQuery);

        if (parentId.HasValue)
            conditions.Add($"[System.Parent] = {parentId.Value}");

        var wiql = $"SELECT [System.Id] FROM WorkItems WHERE {string.Join(" AND ", conditions)} ORDER BY [System.ChangedDate] DESC";

        var wiqlRequest = new RestRequest($"{project}/_apis/wit/wiql", Method.Post);
        wiqlRequest.AddQueryParameter("api-version", API_VERSION);
        wiqlRequest.AddJsonBody(new WiqlRequest { Query = wiql });

        var wiqlResponse = await client.ExecuteAsync(wiqlRequest, cancellationToken);

        if (!wiqlResponse.IsSuccessStatusCode)
            throw new Exception($"Failed to query work items. Status: {wiqlResponse.StatusCode}. {wiqlResponse.Content}");

        var refs = JsonConvert.DeserializeObject<WiqlResponse>(wiqlResponse.Content).WorkItems;

        if (refs is null || refs.Count == 0)
            return ([], 0);

        var totalMatched = refs.Count;
        var selectedIds = refs.Take(Math.Max(top, 1)).Select(r => r.Id).ToList();

        var fetched = new List<WorkItemResponse>(selectedIds.Count);

        foreach (var chunk in selectedIds.Chunk(WorkItemBatchSize))
        {
            var batchRequest = new RestRequest($"{project}/_apis/wit/workitems", Method.Get);
            batchRequest.AddQueryParameter("api-version", API_VERSION);
            batchRequest.AddQueryParameter("ids", string.Join(",", chunk));
            batchRequest.AddQueryParameter("fields", WorkItemFields);

            var batchResponse = await client.ExecuteAsync(batchRequest, cancellationToken);

            if (!batchResponse.IsSuccessStatusCode)
                throw new Exception($"Failed to fetch work item details. Status: {batchResponse.StatusCode}. {batchResponse.Content}");

            var page = JsonConvert.DeserializeObject<WorkItemBatchResponse>(batchResponse.Content)?.Value;
            if (page is not null)
                fetched.AddRange(page);
        }

        // Batch responses are not guaranteed to preserve the order of the requested ids,
        // so restore the ordering produced by the query.
        var byId = fetched.GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First());
        var items = selectedIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();

        return (items, totalMatched);
    }

    public static async Task<string> GetDefaultAreaPath(string project, string team, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/{Uri.EscapeDataString(team)}/_apis/work/teamsettings/teamfieldvalues", Method.Get);
        request.AddQueryParameter("api-version", API_VERSION);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to get area path for team '{team}'. Status: {response.StatusCode}. {response.Content}");

        var result = JsonConvert.DeserializeObject<TeamFieldValuesResponse>(response.Content);
        return result?.DefaultValue
            ?? throw new Exception($"No default area path configured for team '{team}'.");
    }

    public static async Task<WorkItemResponse> CreateWorkItem(string project, string type, List<JsonPatchOperation> operations, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/_apis/wit/workitems/${Uri.EscapeDataString(type)}", Method.Post);
        request.AddQueryParameter("api-version", API_VERSION);
        request.AddHeader("Content-Type", "application/json-patch+json");
        request.AddBody(JsonConvert.SerializeObject(operations), "application/json-patch+json");

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to create work item. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<WorkItemResponse>(response.Content);
    }

    public static async Task<List<PipelineResponse>> ListPipelines(string project, string nameFilter, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/_apis/pipelines", Method.Get);
        request.AddQueryParameter("api-version", API_VERSION);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to list pipelines. Status: {response.StatusCode}. {response.Content}");

        var pipelines = JsonConvert.DeserializeObject<PipelineListResponse>(response.Content).Value ?? [];

        if (!string.IsNullOrEmpty(nameFilter))
            pipelines = pipelines.Where(p => p.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        return pipelines;
    }

    public static async Task<List<PipelineRunResponse>> ListPipelineRuns(string project, int pipelineId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/_apis/pipelines/{pipelineId}/runs", Method.Get);
        request.AddQueryParameter("api-version", API_VERSION);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to list runs for pipeline {pipelineId}. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<PipelineRunListResponse>(response.Content).Value ?? [];
    }

    public static async Task<PipelineRunResponse> QueuePipelineRun(string project, int pipelineId, string branch, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/_apis/pipelines/{pipelineId}/runs", Method.Post);
        request.AddQueryParameter("api-version", API_VERSION);

        string payload = "{}";
        if (!string.IsNullOrEmpty(branch))
        {
            var refName = branch.StartsWith("refs/", StringComparison.OrdinalIgnoreCase) ? branch : $"refs/heads/{branch}";
            var body = new PipelineRunRequest
            {
                Resources = new PipelineRunResources
                {
                    Repositories = new Dictionary<string, PipelineRepositoryResource>
                    {
                        ["self"] = new PipelineRepositoryResource { RefName = refName }
                    }
                }
            };
            payload = JsonConvert.SerializeObject(body);
        }

        request.AddStringBody(payload, DataFormat.Json);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to queue run for pipeline {pipelineId}. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<PipelineRunResponse>(response.Content);
    }

    private static string NormalizeBranch(string branch) =>
        branch.StartsWith("refs/", StringComparison.OrdinalIgnoreCase) ? branch : $"refs/heads/{branch}";

    public static async Task<List<PullRequestResponse>> ListPullRequests(string project, string repo, string status, string targetBranch, int top, string creatorId = null, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var path = string.IsNullOrEmpty(repo)
            ? $"{project}/_apis/git/pullrequests"
            : $"{project}/_apis/git/repositories/{Uri.EscapeDataString(repo)}/pullrequests";

        var request = new RestRequest(path, Method.Get);
        request.AddQueryParameter("api-version", API_VERSION);
        request.AddQueryParameter("$top", top.ToString());
        if (!string.IsNullOrEmpty(status))
            request.AddQueryParameter("searchCriteria.status", status);
        if (!string.IsNullOrEmpty(targetBranch))
            request.AddQueryParameter("searchCriteria.targetRefName", NormalizeBranch(targetBranch));
        if (!string.IsNullOrEmpty(creatorId))
            request.AddQueryParameter("searchCriteria.creatorId", creatorId);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to list pull requests. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<PullRequestListResponse>(response.Content).Value ?? [];
    }

    public static async Task<PullRequestResponse> GetPullRequest(int pullRequestId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"_apis/git/pullrequests/{pullRequestId}", Method.Get);
        request.AddQueryParameter("api-version", API_VERSION);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to get pull request {pullRequestId}. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<PullRequestResponse>(response.Content);
    }

    public static async Task<PullRequestResponse> CreatePullRequest(string project, string repo, string sourceBranch, string targetBranch, string title, string description, bool isDraft, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/_apis/git/repositories/{Uri.EscapeDataString(repo)}/pullrequests", Method.Post);
        request.AddQueryParameter("api-version", API_VERSION);

        var body = new PullRequestCreateRequest
        {
            SourceRefName = NormalizeBranch(sourceBranch),
            TargetRefName = NormalizeBranch(targetBranch),
            Title = title,
            Description = description,
            IsDraft = isDraft
        };
        request.AddStringBody(JsonConvert.SerializeObject(body), DataFormat.Json);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to create pull request. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<PullRequestResponse>(response.Content);
    }

    /// <summary>Casts the current user's vote on a pull request (self-adds as a reviewer if needed).</summary>
    public static async Task VotePullRequest(string project, string repo, int pullRequestId, string userId, int vote, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/_apis/git/repositories/{Uri.EscapeDataString(repo)}/pullrequests/{pullRequestId}/reviewers/{userId}", Method.Put);
        request.AddQueryParameter("api-version", API_VERSION);
        request.AddStringBody(JsonConvert.SerializeObject(new { vote }), DataFormat.Json);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to vote on pull request {pullRequestId}. Status: {response.StatusCode}. {response.Content}");
    }

    /// <summary>Adds a top-level comment thread to a pull request, returning the created thread id.</summary>
    public static async Task<int> AddPullRequestComment(string project, string repo, int pullRequestId, string content, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/_apis/git/repositories/{Uri.EscapeDataString(repo)}/pullrequests/{pullRequestId}/threads", Method.Post);
        request.AddQueryParameter("api-version", API_VERSION);

        var body = new
        {
            comments = new[] { new { parentCommentId = 0, content, commentType = 1 } },
            status = 1
        };
        request.AddStringBody(JsonConvert.SerializeObject(body), DataFormat.Json);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to comment on pull request {pullRequestId}. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<PullRequestThreadResponse>(response.Content)?.Id ?? 0;
    }

    /// <summary>Sets a pull request status (e.g. "abandoned"), returning the updated PR.</summary>
    public static async Task<PullRequestResponse> SetPullRequestStatus(string project, string repo, int pullRequestId, string status, CancellationToken cancellationToken = default)
    {
        return await PatchPullRequest(project, repo, pullRequestId, new { status }, cancellationToken);
    }

    /// <summary>Completes (merges) a pull request using its last merge source commit.</summary>
    public static async Task<PullRequestResponse> CompletePullRequest(string project, string repo, int pullRequestId, string lastMergeSourceCommitId, bool deleteSourceBranch, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            status = "completed",
            lastMergeSourceCommit = new { commitId = lastMergeSourceCommitId },
            completionOptions = new { deleteSourceBranch }
        };
        return await PatchPullRequest(project, repo, pullRequestId, body, cancellationToken);
    }

    private static async Task<PullRequestResponse> PatchPullRequest(string project, string repo, int pullRequestId, object body, CancellationToken cancellationToken)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/_apis/git/repositories/{Uri.EscapeDataString(repo)}/pullrequests/{pullRequestId}", Method.Patch);
        request.AddQueryParameter("api-version", API_VERSION);
        request.AddStringBody(JsonConvert.SerializeObject(body), DataFormat.Json);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to update pull request {pullRequestId}. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<PullRequestResponse>(response.Content);
    }

    public static async Task<AuthenticatedUser> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest("_apis/connectiondata", Method.Get);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to get current user. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<ConnectionDataResponse>(response.Content)?.AuthenticatedUser
            ?? throw new Exception("Could not read authenticated user from connection data.");
    }

    public static async Task<IterationResponse> GetCurrentIteration(string project, string team, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/{Uri.EscapeDataString(team)}/_apis/work/teamsettings/iterations", Method.Get);
        request.AddQueryParameter("api-version", API_VERSION);
        request.AddQueryParameter("$timeframe", "current");

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to get current iteration. Status: {response.StatusCode}. {response.Content}");

        var list = JsonConvert.DeserializeObject<IterationListResponse>(response.Content);
        return list?.Value?.FirstOrDefault()
            ?? throw new Exception($"No active iteration found for team '{team}' in project '{project}'.");
    }

    public static async Task<WorkItemResponse> UpdateWorkItem(int id, string project, List<JsonPatchOperation> operations, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/_apis/wit/workitems/{id}", Method.Patch);
        request.AddQueryParameter("api-version", API_VERSION);
        request.AddHeader("Content-Type", "application/json-patch+json");
        request.AddBody(JsonConvert.SerializeObject(operations), "application/json-patch+json");

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to update work item {id}. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<WorkItemResponse>(response.Content);
    }

    /// <summary>Deletes a work item, moving it to the project recycle bin (recoverable).</summary>
    public static async Task DeleteWorkItem(int id, string project, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var request = new RestRequest($"{project}/_apis/wit/workitems/{id}", Method.Delete);
        request.AddQueryParameter("api-version", API_VERSION);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to delete work item {id}. Status: {response.StatusCode}. {response.Content}");
    }
}
