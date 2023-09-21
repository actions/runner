using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Linq;

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
        public List<Runner> Runners
        {
            get;
            set;
        }

        public ListRunnersResponse Clone()
        {
            return new ListRunnersResponse(this);
        }

        public List<TaskAgent> ToTaskAgents()
        {
            return Runners.Select(runner => new TaskAgent() { Id = runner.Id, Name = runner.Name }).ToList();
        }
    }

}
