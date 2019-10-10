using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.WebApi
{
    public static class JobEventTypes
    {
        public const String JobAssigned = "JobAssigned";

        public const String JobCompleted = "JobCompleted";

        public const String JobStarted = "JobStarted";

        public const String TaskAssigned = "TaskAssigned";

        public const String TaskStarted = "TaskStarted";

        public const String TaskCompleted = "TaskCompleted";
    }

    [DataContract]
    [KnownType(typeof(JobAssignedEvent))]
    [KnownType(typeof(JobCompletedEvent))]
    [KnownType(typeof(JobStartedEvent))]
    [KnownType(typeof(TaskAssignedEvent))]
    [KnownType(typeof(TaskStartedEvent))]
    [KnownType(typeof(TaskCompletedEvent))]
    [KnownType(typeof(TaskLocalExecutionCompletedEvent))]
    [JsonConverter(typeof(JobEventJsonConverter))]
    public abstract class JobEvent
    {
        protected JobEvent(String name)
        {
            this.Name = name;
        }

        protected JobEvent(
            String name, 
            Guid jobId)
        {
            this.Name = name;
            this.JobId = jobId;
        }

        [DataMember]
        public String Name
        {
            get;
            private set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid JobId
        {
            get;
            set;
        }
    }

    [DataContract]
    public sealed class JobAssignedEvent : JobEvent
    {
        internal JobAssignedEvent()
            : base(JobEventTypes.JobAssigned)
        {
        }

        public JobAssignedEvent(Guid jobId)
            : base(JobEventTypes.JobAssigned, jobId)
        {
        }

        public JobAssignedEvent(
            Guid jobId,
            TaskAgentJobRequest request)
            : base(JobEventTypes.JobAssigned, jobId)
        {
            this.Request = request;
        }

        [DataMember]
        public TaskAgentJobRequest Request
        {
            get;
            set;
        }
    }

    [DataContract]
    public sealed class JobStartedEvent : JobEvent
    {
        internal JobStartedEvent()
            : base(JobEventTypes.JobStarted)
        {
        }

        public JobStartedEvent(Guid jobId)
            : base(JobEventTypes.JobStarted, jobId)
        {
        }
    }

    [DataContract]
    public sealed class JobCompletedEvent : JobEvent
    {
        internal JobCompletedEvent()
            : base(JobEventTypes.JobCompleted)
        {
        }

        public JobCompletedEvent(
            Guid jobId,
            TaskResult result)
            : this(0, jobId, result)
        {
        }

        public JobCompletedEvent(
            Int64 requestId,
            Guid jobId,
            TaskResult result)
            : this(requestId, jobId, result, null)
        {
        }

        public JobCompletedEvent(
            Int64 requestId,
            Guid jobId,
            TaskResult result,
            IDictionary<String, VariableValue> outputVariables)
            : base(JobEventTypes.JobCompleted, jobId)
        {
            this.RequestId = requestId;
            this.Result = result;
        }

        [DataMember(EmitDefaultValue = false)]
        public Int64 RequestId
        {
            get;
            set;
        }

        [DataMember]
        public TaskResult Result
        {
            get;
            set;
        }
    }

    [DataContract]
    public abstract class TaskEvent : JobEvent
    {
        protected TaskEvent(string name) : base(name)
        {
        }

        protected TaskEvent(
            string name, 
            Guid jobId, 
            Guid taskId) 
            : base(name, jobId)
        {
            TaskId = taskId;
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid TaskId
        {
            get;
            set;
        }
    }

    [DataContract]
    public sealed class TaskAssignedEvent : TaskEvent
    {
        public TaskAssignedEvent() 
            : base(JobEventTypes.TaskAssigned)
        {
        }

        public TaskAssignedEvent(
            Guid jobId, 
            Guid taskId) 
            : base(JobEventTypes.TaskAssigned, jobId, taskId)
        {
        }
    }

    [DataContract]
    public sealed class TaskStartedEvent : TaskEvent
    {
        public TaskStartedEvent() 
            : base(JobEventTypes.TaskStarted)
        {
        }

        public TaskStartedEvent(
            Guid jobId, 
            Guid taskId) 
            : base(JobEventTypes.TaskStarted, jobId, taskId)
        {
        }
    }

    [DataContract]
    public sealed class TaskCompletedEvent : TaskEvent
    {
        public TaskCompletedEvent() 
            : base(JobEventTypes.TaskCompleted)
        {
        }

        public TaskCompletedEvent(
            Guid jobId, 
            Guid taskId, 
            TaskResult taskResult) 
            : base(JobEventTypes.TaskCompleted, jobId, taskId)
        {
            Result = taskResult;
        }

        [DataMember]
        public TaskResult Result
        {
            get;
            set;
        }
    }

    [DataContract]
    [ClientIgnore]
    internal sealed class TaskLocalExecutionCompletedEvent : TaskEvent
    {
        public TaskLocalExecutionCompletedEvent()
            : base(JobEventTypes.TaskCompleted)
        {
        }

        public TaskLocalExecutionCompletedEvent(
            Guid jobId,
            Guid taskId,
            ServerTaskSectionExecutionOutput data)
            : base(JobEventTypes.TaskCompleted, jobId, taskId)
        {
            EventData = data;
        }

        [DataMember]
        public ServerTaskSectionExecutionOutput EventData
        {
            get;
            set;
        }
    }

    internal sealed class JobEventJsonConverter : VssSecureJsonConverter
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
            return typeof(JobEvent).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override Object ReadJson(
            JsonReader reader, 
            Type objectType, 
            Object existingValue, 
            JsonSerializer serializer)
        {
            var eventObject = JObject.Load(reader);

            JToken propertyValue;
            JobEvent jobEvent = null;
            if (eventObject.TryGetValue("Name", StringComparison.OrdinalIgnoreCase, out propertyValue))
            {
                if (propertyValue.Type == JTokenType.String)
                {
                    String nameValue = (String)propertyValue;
                    if (String.Equals(nameValue, JobEventTypes.JobAssigned, StringComparison.Ordinal))
                    {
                        jobEvent = new JobAssignedEvent();
                    }
                    else if (String.Equals(nameValue, JobEventTypes.JobCompleted, StringComparison.Ordinal))
                    {
                        jobEvent = new JobCompletedEvent();
                    }
                    else if (String.Equals(nameValue, JobEventTypes.JobStarted, StringComparison.Ordinal))
                    {
                        jobEvent = new JobStartedEvent();
                    }
                    else if (String.Equals(nameValue, JobEventTypes.TaskAssigned, StringComparison.Ordinal))
                    {
                        jobEvent = new TaskAssignedEvent();
                    }
                    else if (String.Equals(nameValue, JobEventTypes.TaskStarted, StringComparison.Ordinal))
                    {
                        jobEvent = new TaskStartedEvent();
                    }
                    else if (String.Equals(nameValue, JobEventTypes.TaskCompleted, StringComparison.Ordinal))
                    {
                        jobEvent = new TaskCompletedEvent();
                    }
                }
            }

            if (jobEvent == null)
            {
                if (eventObject.TryGetValue("Request", StringComparison.OrdinalIgnoreCase, out propertyValue))
                {
                    jobEvent = new JobAssignedEvent();
                }
                else if (eventObject.TryGetValue("Result", StringComparison.OrdinalIgnoreCase, out propertyValue))
                {
                    jobEvent = new JobCompletedEvent();
                }
            }

            if (jobEvent == null)
            {
                return existingValue;
            }

            using (var objectReader = eventObject.CreateReader())
            {
                serializer.Populate(objectReader, jobEvent);
            }

            return jobEvent;
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
