using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    using Microsoft.VisualStudio.Services.WebApi;
    using Microsoft.VisualStudio.Services.WebApi.Internal;

    [JsonConverter(typeof(EventsConfigJsonConverter))]
    [KnownType(typeof(JobEventsConfig))]
    [KnownType(typeof(TaskEventConfig))]
    public abstract class EventsConfig
    {
    }

    [ClientIgnore]
    [DataContract]
    public sealed class TaskEventsConfig : EventsConfig
    {
        [DataMember(EmitDefaultValue = false)]
        public TaskEventConfig TaskAssigned
        {
            get
            {
                return GetEvent(nameof(TaskAssigned));
            }

            set
            {
                SetEvent(nameof(TaskAssigned), value);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskEventConfig TaskStarted
        {
            get
            {
                return GetEvent(nameof(TaskStarted));
            }

            set
            {
                SetEvent(nameof(TaskStarted), value);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskEventConfig TaskCompleted
        {
            get
            {
                return GetEvent(nameof(TaskCompleted));
            }

            set
            {
                SetEvent(nameof(TaskCompleted), value);
            }
        }

        public Dictionary<String, TaskEventConfig> All
        {
            get
            {
                return m_taskEvents;
            }
        }

        private void SetEvent(String name, TaskEventConfig taskEventConfig)
        {
            m_taskEvents[name] = taskEventConfig;
        }

        private TaskEventConfig GetEvent(String name)
        {
            TaskEventConfig taskEventConfig;
            if (m_taskEvents.TryGetValue(name, out taskEventConfig))
            {
                return taskEventConfig;
            }

            return null;
        }

        private Dictionary<String, TaskEventConfig> m_taskEvents = new Dictionary<String, TaskEventConfig>();
    }

    [DataContract]
    public class JobEventsConfig : EventsConfig
    {
        [DataMember(EmitDefaultValue = false)]
        public JobEventConfig JobAssigned
        {
            get
            {
                return GetEvent(nameof(JobAssigned));
            }

            set
            {
                SetEvent(nameof(JobAssigned), value);    
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public JobEventConfig JobStarted
        {
            get
            {
                return GetEvent(nameof(JobStarted));
            }

            set
            {
                SetEvent(nameof(JobStarted), value);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public JobEventConfig JobCompleted
        {
            get
            {
                return GetEvent(nameof(JobCompleted));
            }

            set
            {
                SetEvent(nameof(JobCompleted), value);
            }
        }

        public Dictionary<string, JobEventConfig> All
        {
            get
            {
                return m_jobEvents;
            }
        }

        private void SetEvent(String name, JobEventConfig jobEventConfig)
        {
            m_jobEvents[name] = jobEventConfig;
        }

        private JobEventConfig GetEvent(String name)
        {
            JobEventConfig jobEventConfig;
            if (m_jobEvents.TryGetValue(name, out jobEventConfig))
            {
                return jobEventConfig;
            }
            return null;
        }

        private Dictionary<String, JobEventConfig> m_jobEvents = new Dictionary<String, JobEventConfig>(); 
    }

    internal sealed class EventsConfigJsonConverter : VssSecureJsonConverter
    {
        public override Boolean CanWrite
        {
            get
            {
                return false;
            }
        }

        public override Boolean CanConvert(Type objectType)
        {
            return typeof(EventsConfig).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override Object ReadJson(
            JsonReader reader,
            Type objectType,
            Object existingValue,
            JsonSerializer serializer)
        {
            var eventObject = JObject.Load(reader);

            JToken propertyValue;
            EventsConfig eventsConfig = null;
            if (eventObject.TryGetValue(JobEventTypes.JobAssigned, StringComparison.OrdinalIgnoreCase, out propertyValue)
                || eventObject.TryGetValue(JobEventTypes.JobStarted, StringComparison.OrdinalIgnoreCase, out propertyValue)
                || eventObject.TryGetValue(JobEventTypes.JobCompleted, StringComparison.OrdinalIgnoreCase, out propertyValue))
            {
                eventsConfig = new JobEventsConfig();
            }
            else if (eventObject.TryGetValue(JobEventTypes.TaskAssigned, StringComparison.OrdinalIgnoreCase, out propertyValue)
                || eventObject.TryGetValue(JobEventTypes.TaskStarted, StringComparison.OrdinalIgnoreCase, out propertyValue)
                || eventObject.TryGetValue(JobEventTypes.TaskCompleted, StringComparison.OrdinalIgnoreCase, out propertyValue))
            {
                eventsConfig = new TaskEventsConfig();
            }

            if (eventsConfig == null)
            {
                return existingValue;
            }

            using (var objectReader = eventObject.CreateReader())
            {
                serializer.Populate(objectReader, eventsConfig);
            }

            return eventsConfig;
        }

        public override void WriteJson(
            JsonWriter writer,
            Object value,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}