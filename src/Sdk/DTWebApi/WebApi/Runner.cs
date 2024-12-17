using System;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    public class Runner
    {

        public class Authorization
        {
            /// <summary>
            /// The url to refresh tokens
            /// </summary> 
            [JsonProperty("authorization_url")]
            public Uri AuthorizationUrl
            {
                get;
                internal set;
            }

            /// <summary>
            /// The url to connect to to poll for messages
            /// </summary> 
            [JsonProperty("server_url")]
            public string ServerUrl
            {
                get;
                internal set;
            }

            /// <summary>
            /// The client id to use when connecting to the authorization_url
            /// </summary>
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
        public ulong Id
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
