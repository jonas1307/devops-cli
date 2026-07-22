using Newtonsoft.Json;

namespace DevOps.Requests;

public class JsonPatchOperation
{
    [JsonProperty("op")]
    public string Op { get; set; }

    [JsonProperty("path")]
    public string Path { get; set; }

    [JsonProperty("value")]
    public object Value { get; set; }
}

public class WiqlRequest
{
    [JsonProperty("query")]
    public string Query { get; set; }
}

public class WorkItemRelation
{
    [JsonProperty("rel")]
    public string Rel { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
}

public class PipelineRunRequest
{
    [JsonProperty("resources")]
    public PipelineRunResources Resources { get; set; }
}

public class PipelineRunResources
{
    [JsonProperty("repositories")]
    public Dictionary<string, PipelineRepositoryResource> Repositories { get; set; }
}

public class PipelineRepositoryResource
{
    [JsonProperty("refName")]
    public string RefName { get; set; }
}
