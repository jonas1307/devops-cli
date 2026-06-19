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
