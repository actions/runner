using Newtonsoft.Json;
using System.Security.AccessControl;

namespace GitHub.DistributedTask.WebApi
{
    public class Runner
    {

        public class Authorization
        {
            [JsonProperty("authorization_url")]
            public Uri AuthorizationUrl
            {
                get;
                internal set;
            }

            [JsonProperty("client_id")]
            public string ClientId
            {
                get;
                internal set;
            }
        }

        [JsonProperty("name")]
        public string Name
        {
            get;
            internal set;
        }

        [JsonProperty("id")]
        public Int32 Id
        {
            get;
            internal set;
        }

        [JsonProperty("authorization")]
        public Authorization RunnerAuthorization
        {
            get;
            internal set;
        }
    }
}
