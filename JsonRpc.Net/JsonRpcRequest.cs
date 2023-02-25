using Newtonsoft.Json;

namespace JsonRpc.Net
{
    /// <summary>
    /// Represents a JsonRpc request
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class JsonRpcRequest
    {
        public JsonRpcRequest()
        {
        }

        public JsonRpcRequest(string method, object pars, object id)
        {
            Method = method;
            Params = pars;
            Id = id;
        }

        [JsonProperty("jsonrpc")]
        public string JsonRpc
        {
            get { return "2.0"; }
        }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public object Params { get; set; }

        [JsonProperty("id")]
        public object Id { get; set; }
    }
}

