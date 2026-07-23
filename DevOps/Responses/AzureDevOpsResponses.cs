using Newtonsoft.Json;

namespace DevOps.Responses;

public class WorkItemResponse
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("fields")]
    public WorkItemFields Fields { get; set; }
}

public class WorkItemFields
{
    [JsonProperty("System.Title")]
    public string Title { get; set; }

    [JsonProperty("System.State")]
    public string State { get; set; }

    [JsonProperty("System.WorkItemType")]
    public string WorkItemType { get; set; }

    [JsonProperty("System.AssignedTo")]
    public AssignedTo AssignedTo { get; set; }

    [JsonProperty("System.Description")]
    public string Description { get; set; }

    [JsonProperty("Microsoft.VSTS.Common.Priority")]
    public int? Priority { get; set; }

    [JsonProperty("System.CreatedDate")]
    public DateTime CreatedDate { get; set; }

    [JsonProperty("System.ChangedDate")]
    public DateTime ChangedDate { get; set; }

    [JsonProperty("System.TeamProject")]
    public string TeamProject { get; set; }

    [JsonProperty("System.Parent")]
    public int? ParentId { get; set; }
}

public class AssignedTo
{
    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("uniqueName")]
    public string UniqueName { get; set; }
}

public class WiqlResponse
{
    [JsonProperty("workItems")]
    public List<WiqlWorkItemRef> WorkItems { get; set; }
}

public class WiqlWorkItemRef
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
}

public class WorkItemBatchResponse
{
    [JsonProperty("value")]
    public List<WorkItemResponse> Value { get; set; }
}

public class IterationListResponse
{
    [JsonProperty("value")]
    public List<IterationResponse> Value { get; set; }
}

public class IterationResponse
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("path")]
    public string Path { get; set; }

    [JsonProperty("attributes")]
    public IterationAttributes Attributes { get; set; }
}

public class IterationAttributes
{
    [JsonProperty("startDate")]
    public DateTime? StartDate { get; set; }

    [JsonProperty("finishDate")]
    public DateTime? FinishDate { get; set; }

    [JsonProperty("timeFrame")]
    public string TimeFrame { get; set; }
}

public class PipelineListResponse
{
    [JsonProperty("value")]
    public List<PipelineResponse> Value { get; set; }
}

public class PipelineResponse
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("folder")]
    public string Folder { get; set; }

    [JsonProperty("revision")]
    public int Revision { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
}

public class PipelineRunListResponse
{
    [JsonProperty("value")]
    public List<PipelineRunResponse> Value { get; set; }
}

public class PipelineRunResponse
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("state")]
    public string State { get; set; }

    [JsonProperty("result")]
    public string Result { get; set; }

    [JsonProperty("createdDate")]
    public DateTime? CreatedDate { get; set; }

    [JsonProperty("finishedDate")]
    public DateTime? FinishedDate { get; set; }

    [JsonProperty("_links")]
    public PipelineRunLinks Links { get; set; }
}

public class PipelineRunLinks
{
    [JsonProperty("web")]
    public PipelineRunLink Web { get; set; }
}

public class PipelineRunLink
{
    [JsonProperty("href")]
    public string Href { get; set; }
}

public class PullRequestListResponse
{
    [JsonProperty("value")]
    public List<PullRequestResponse> Value { get; set; }
}

public class PullRequestResponse
{
    [JsonProperty("pullRequestId")]
    public int PullRequestId { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("isDraft")]
    public bool IsDraft { get; set; }

    [JsonProperty("sourceRefName")]
    public string SourceRefName { get; set; }

    [JsonProperty("targetRefName")]
    public string TargetRefName { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("creationDate")]
    public DateTime? CreationDate { get; set; }

    [JsonProperty("createdBy")]
    public AssignedTo CreatedBy { get; set; }

    [JsonProperty("repository")]
    public PullRequestRepository Repository { get; set; }

    [JsonProperty("reviewers")]
    public List<PullRequestReviewer> Reviewers { get; set; }

    [JsonProperty("lastMergeSourceCommit")]
    public GitCommitRef LastMergeSourceCommit { get; set; }

    [JsonProperty("_links")]
    public PullRequestLinks Links { get; set; }
}

public class GitCommitRef
{
    [JsonProperty("commitId")]
    public string CommitId { get; set; }
}

public class PullRequestRepository
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("project")]
    public PullRequestProject Project { get; set; }
}

public class PullRequestProject
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class PullRequestThreadResponse
{
    [JsonProperty("id")]
    public int Id { get; set; }
}

public class IdentityListResponse
{
    [JsonProperty("value")]
    public List<IdentityRef> Value { get; set; }
}

public class IdentityRef
{
    [JsonProperty("id")]
    public string Id { get; set; }
}

public class PullRequestReviewer
{
    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("vote")]
    public int Vote { get; set; }

    [JsonProperty("isRequired")]
    public bool IsRequired { get; set; }
}

public class PullRequestLinks
{
    [JsonProperty("web")]
    public PullRequestWebLink Web { get; set; }
}

public class PullRequestWebLink
{
    [JsonProperty("href")]
    public string Href { get; set; }
}

public class TeamFieldValuesResponse
{
    [JsonProperty("defaultValue")]
    public string DefaultValue { get; set; }
}

public class ConnectionDataResponse
{
    [JsonProperty("authenticatedUser")]
    public AuthenticatedUser AuthenticatedUser { get; set; }
}

public class AuthenticatedUser
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("providerDisplayName")]
    public string DisplayName { get; set; }

    [JsonProperty("properties")]
    public IdentityProperties Properties { get; set; }
}

public class IdentityProperties
{
    [JsonProperty("Account")]
    public IdentityPropertyValue Account { get; set; }
}

public class IdentityPropertyValue
{
    [JsonProperty("$value")]
    public string Value { get; set; }
}
