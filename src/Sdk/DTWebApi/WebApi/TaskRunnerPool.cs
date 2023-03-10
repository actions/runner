using GitHub.Services.WebApi;
using System;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// An organization-level grouping of runners.
    /// </summary>
    [DataContract]
    public class TaskRunnerGroup
    {
        internal TaskRunnerGroup()
        {
        }

        public TaskRunnerGroup(String name)
        {
            this.Name = name;
        }

        private TaskRunnerGroup(TaskRunnerGroup poolToBeCloned)
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
            this.RunnerGroups = new List<TaskRunnerGroup>();
        }

        public List<TaskAgentPool> ToAgentPoolList()
        {
            List<TaskAgentPool> agentPools = new List<TaskAgentPool>();
            foreach (TaskRunnerGroup runnerGroup in this.RunnerGroups)
            {
                TaskAgentPool agentPool = new TaskAgentPool(runnerGroup.Name);
                agentPool.Id = runnerGroup.Id;
                agentPool.IsHosted = runnerGroup.IsHosted;
                agentPool.IsInternal = runnerGroup.IsDefault;
                agentPools.Add(agentPool);
            }

            return agentPools;
        }

        [JsonProperty("runner_groups")]
        public List<TaskRunnerGroup> RunnerGroups { get; set; }

        [JsonProperty("total_count")]
        public int Count { get; set; }
    }
}
