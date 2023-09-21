using GitHub.Services.WebApi;
using System;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// An organization-level grouping of runners.
    /// </summary>
    [DataContract]
    public class RunnerGroup
    {
        internal RunnerGroup()
        {
        }

        public RunnerGroup(String name)
        {
            this.Name = name;
        }

        private RunnerGroup(RunnerGroup poolToBeCloned)
        {
            this.Id = poolToBeCloned.Id;
            this.IsHosted = poolToBeCloned.IsHosted;
            this.Name = poolToBeCloned.Name;
            this.IsDefault = poolToBeCloned.IsDefault;
        }

        [DataMember(EmitDefaultValue = false)]
        [JsonProperty("id")]
        public Int32 Id
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        [JsonProperty("name")]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not this pool is internal and can't be modified by users
        /// </summary>
        [DataMember]
        [JsonProperty("default")]
        public bool IsDefault
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not this pool is managed by the service.
        /// </summary>
        [DataMember]
        [JsonProperty("is_hosted")]
        public Boolean IsHosted
        {
            get;
            set;
        }
    }

    public class RunnerGroupList
    {
        public RunnerGroupList()
        {
            this.RunnerGroups = new List<RunnerGroup>();
        }

        public List<TaskAgentPool> ToAgentPoolList()
        {
            var agentPools = this.RunnerGroups.Select(x => new TaskAgentPool(x.Name)
            {
                Id = x.Id,
                IsHosted = x.IsHosted,
                IsInternal = x.IsDefault
            }).ToList();

            return agentPools;
        }

        [JsonProperty("runner_groups")]
        public List<RunnerGroup> RunnerGroups { get; set; }

        [JsonProperty("total_count")]
        public int Count { get; set; }
    }
}
