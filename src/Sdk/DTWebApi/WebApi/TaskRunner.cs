using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{

    public class ListRunnersResponse
    {
        public ListRunnersResponse()
        {
        }

        public ListRunnersResponse(ListRunnersResponse responseToBeCloned)
        {
            this.TotalCount = responseToBeCloned.TotalCount;
            this.Runners = responseToBeCloned.Runners;
        }

        [JsonProperty("total_count")]
        public int TotalCount
        {
            get;
            set;
        }

        [JsonProperty("runners")]
        public List<TaskAgent> Runners
        {
            get;
            set;
        }

        public ListRunnersResponse Clone()
        {
            return new ListRunnersResponse(this);
        }
    }
        
}
