using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public enum TaskAgentUpdateReasonType
    {
        [EnumMember]
        Manual = 1,

        [EnumMember]
        MinAgentVersionRequired = 2,
    }

    internal sealed class TaskAgentUpdateReasonJsonConverter : VssSecureJsonConverter
    {
        public override Boolean CanConvert(Type objectType)
        {
            return typeof(TaskAgentUpdateReason).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override Boolean CanWrite
        {
            get
            {
                return false;
            }
        }

        public override Object ReadJson(
            JsonReader reader,
            Type objectType,
            Object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            Object newValue = null;
            JToken propertyValue;
            JObject value = JObject.Load(reader);

            if (value.TryGetValue("Code", StringComparison.OrdinalIgnoreCase, out propertyValue))
            {
                if (propertyValue.Type == JTokenType.String)
                {
                    TaskAgentUpdateReasonType code;
                    if (Enum.TryParse<TaskAgentUpdateReasonType>((String)propertyValue, out code))
                    {
                        switch (code)
                        {
                            case TaskAgentUpdateReasonType.Manual:
                                newValue = new TaskAgentManualUpdate();
                                break;

                            case TaskAgentUpdateReasonType.MinAgentVersionRequired:
                                newValue = new TaskAgentMinAgentVersionRequiredUpdate();
                                break;
                        }

                    }
                }
            }

            if (newValue == null)
            {
                return existingValue;
            }

            using (JsonReader objectReader = value.CreateReader())
            {
                serializer.Populate(objectReader, newValue);
            }

            return newValue;
        }

        public override void WriteJson(
            JsonWriter writer,
            Object value,
            JsonSerializer serializer)
        {
            // The virtual method returns false for CanWrite so this should never be invoked
            throw new NotSupportedException();
        }
    }

    [DataContract]
    [ServiceEventObjectAttribute]
    [JsonConverter(typeof(TaskAgentUpdateReasonJsonConverter))]
    public abstract class TaskAgentUpdateReason
    {
        protected TaskAgentUpdateReason(TaskAgentUpdateReasonType code)
        {
            this.Code = code;
        }

        [DataMember]
        public TaskAgentUpdateReasonType Code { get; private set; }
    }

    [DataContract]
    public class TaskAgentManualUpdate : TaskAgentUpdateReason
    {
        [JsonConstructor]
        internal TaskAgentManualUpdate() :
            base(TaskAgentUpdateReasonType.Manual)
        {
        }

        public TaskAgentManualUpdate Clone()
        {
            return new TaskAgentManualUpdate();
        }
    }

    [DataContract]
    public class TaskAgentMinAgentVersionRequiredUpdate : TaskAgentUpdateReason
    {
        [JsonConstructor]
        internal TaskAgentMinAgentVersionRequiredUpdate() :
            base(TaskAgentUpdateReasonType.MinAgentVersionRequired)
        {
        }

        private TaskAgentMinAgentVersionRequiredUpdate(TaskAgentMinAgentVersionRequiredUpdate updateToBeCloned) :
            base(TaskAgentUpdateReasonType.MinAgentVersionRequired)
        {
            if (updateToBeCloned.MinAgentVersion != null)
            {
                this.MinAgentVersion = updateToBeCloned.MinAgentVersion.Clone();
            }
            if (updateToBeCloned.JobDefinition != null)
            {
                this.JobDefinition = updateToBeCloned.JobDefinition.Clone();
            }
            if (updateToBeCloned.JobOwner != null)
            {
                this.JobOwner = updateToBeCloned.JobOwner.Clone();
            }
        }

        [DataMember]
        public Demand MinAgentVersion
        {
            get;
            set;
        }

        [DataMember]
        public TaskOrchestrationOwner JobDefinition
        {
            get;
            set;
        }

        [DataMember]
        public TaskOrchestrationOwner JobOwner
        {
            get;
            set;
        }

        public TaskAgentMinAgentVersionRequiredUpdate Clone()
        {
            return new TaskAgentMinAgentVersionRequiredUpdate(this);
        }
    }
}
