using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [ClientIgnore]
    [DataContract]
    public sealed class ExecuteTaskResponse
    {
        [JsonConstructor]
        public ExecuteTaskResponse()
        {
        }

        public ExecuteTaskResponse(EventsConfig eventsConfig)
        {
            var jobEventsConfig = eventsConfig as JobEventsConfig;

            if (jobEventsConfig != null)
            {
                m_taskEventsConfig = ToTaskEvents(jobEventsConfig);
            }
            else
            {
                var taskEventsConfig = eventsConfig as TaskEventsConfig;
                if (taskEventsConfig != null)
                {
                    m_taskEventsConfig = taskEventsConfig;
                }
            }
        }

        [DataMember]
        public TaskEventsConfig TaskEvents
        {
            get
            {
                if (m_taskEventsConfig == null)
                {
                    m_taskEventsConfig = new TaskEventsConfig();
                }

                return m_taskEventsConfig;
            }

            internal set
            {
                m_taskEventsConfig = value;
            }
        }

        [DataMember]
        public Boolean WaitForLocalExecutionComplete
        {
            get;
            set;
        }

        internal static TaskEventsConfig ToTaskEvents(JobEventsConfig jobEventsConfig)
        {
            Dictionary<string, JobEventConfig> jobEvents = jobEventsConfig.All;
            var taskEventsConfig = new TaskEventsConfig();
            foreach (var key in jobEvents.Keys)
            {
                var jobEventConfig = jobEvents[key];
                var taskEventConfig = new TaskEventConfig(jobEventConfig.Timeout, true);

                switch (key)
                {
                    case JobEventTypes.JobAssigned:
                        taskEventsConfig.TaskAssigned = taskEventConfig;
                        break;

                    case JobEventTypes.JobStarted:
                        taskEventsConfig.TaskStarted = taskEventConfig;
                        break;

                    case JobEventTypes.JobCompleted:
                        taskEventsConfig.TaskCompleted = taskEventConfig;
                        break;
                }
            }

            return taskEventsConfig;
        }

        private TaskEventsConfig m_taskEventsConfig;
    }
}