using DevOps.Requests;
using DevOps.Responses;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace DevOps.Services;

public static class HttpService
{
    private const string API_VERSION = "7.1";

    private static RestClient CreateClient()
    {
        var config = ConfigService.LoadConfig();
        var options = new RestClientOptions(config.OrgUrl)
        {
            Authenticator = new HttpBasicAuthenticator(string.Empty, config.Pat)
        };
        return new RestClient(options);
    }

    public static async Task<WorkItemResponse> GetWorkItem(int id, string project, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var request = new RestRequest($"{project}/_apis/wit/workitems/{id}", Method.Get);
        request.AddQueryParameter("api-version", API_VERSION);
        request.AddQueryParameter("$expand", "all");

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to get work item {id}. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<WorkItemResponse>(response.Content);
    }

    public static async Task<List<WorkItemResponse>> ListWorkItems(string project, string state, string type, string assignedTo, string customQuery, int? parentId = null, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();

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
            return [];

        var ids = string.Join(",", refs.Take(200).Select(r => r.Id));
        var batchRequest = new RestRequest($"{project}/_apis/wit/workitems", Method.Get);
        batchRequest.AddQueryParameter("api-version", API_VERSION);
        batchRequest.AddQueryParameter("ids", ids);
        batchRequest.AddQueryParameter("fields",
            "System.Id,System.Title,System.State,System.WorkItemType,System.AssignedTo," +
            "System.Description,Microsoft.VSTS.Common.Priority,System.CreatedDate," +
            "System.ChangedDate,System.TeamProject,System.Parent");

        var batchResponse = await client.ExecuteAsync(batchRequest, cancellationToken);

        if (!batchResponse.IsSuccessStatusCode)
            throw new Exception($"Failed to fetch work item details. Status: {batchResponse.StatusCode}. {batchResponse.Content}");

        return JsonConvert.DeserializeObject<WorkItemBatchResponse>(batchResponse.Content).Value;
    }

    public static async Task<string> GetDefaultAreaPath(string project, string team, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
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
        using var client = CreateClient();
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
        using var client = CreateClient();
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

    public static async Task<AuthenticatedUser> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var request = new RestRequest("_apis/connectiondata", Method.Get);

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to get current user. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<ConnectionDataResponse>(response.Content)?.AuthenticatedUser
            ?? throw new Exception("Could not read authenticated user from connection data.");
    }

    public static async Task<IterationResponse> GetCurrentIteration(string project, string team, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
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
        using var client = CreateClient();
        var request = new RestRequest($"{project}/_apis/wit/workitems/{id}", Method.Patch);
        request.AddQueryParameter("api-version", API_VERSION);
        request.AddHeader("Content-Type", "application/json-patch+json");
        request.AddBody(JsonConvert.SerializeObject(operations), "application/json-patch+json");

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to update work item {id}. Status: {response.StatusCode}. {response.Content}");

        return JsonConvert.DeserializeObject<WorkItemResponse>(response.Content);
    }
}
