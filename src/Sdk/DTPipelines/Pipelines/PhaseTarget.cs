using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Pipelines.Runtime;
using GitHub.DistributedTask.Pipelines.Validation;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [KnownType(typeof(AgentQueueTarget))]
    [KnownType(typeof(AgentPoolTarget))]
    [KnownType(typeof(ServerTarget))]
    [KnownType(typeof(DeploymentGroupTarget))]
    [JsonConverter(typeof(PhaseTargetJsonConverter))]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class PhaseTarget
    {
        protected PhaseTarget(PhaseTargetType type)
        {
            this.Type = type;
        }

        protected PhaseTarget(PhaseTarget targetToClone)
        {
            this.Type = targetToClone.Type;
            this.ContinueOnError = targetToClone.ContinueOnError;
            this.TimeoutInMinutes = targetToClone.TimeoutInMinutes;
            this.CancelTimeoutInMinutes = targetToClone.CancelTimeoutInMinutes;
            if (targetToClone.m_demands?.Count > 0)
            {
                m_demands = new HashSet<Demand>(targetToClone.m_demands.Select(x => x.Clone()));
            }
        }

        /// <summary>
        /// Get the type of target.
        /// </summary>
        [DataMember]
        public PhaseTargetType Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value which determines whether or not to treat errors as terminal.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [JsonConverter(typeof(ExpressionValueJsonConverter<Boolean>))]
        public ExpressionValue<Boolean> ContinueOnError
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value which determines the maximum amount of time a job is allowed to execute.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [JsonConverter(typeof(ExpressionValueJsonConverter<Int32>))]
        public ExpressionValue<Int32> TimeoutInMinutes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value which determines the maximum amount of time a job is allowed for cancellation.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [JsonConverter(typeof(ExpressionValueJsonConverter<Int32>))]
        public ExpressionValue<Int32> CancelTimeoutInMinutes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the demands which determine where this job may be run.
        /// </summary>
        public ISet<Demand> Demands
        {
            get
            {
                if (m_demands == null)
                {
                    m_demands = new HashSet<Demand>();
                }
                return m_demands;
            }
        }

        /// <summary>
        /// Creates a deep copy of the current instance.
        /// </summary>
        /// <returns>A new <c>PhaseTarget</c> instance of the current type</returns>
        public abstract PhaseTarget Clone();

        /// <summary>
        /// Indicates whether the task definition can run on the target.
        /// </summary>
        public abstract Boolean IsValid(TaskDefinition task);

        internal abstract JobExecutionContext CreateJobContext(PhaseExecutionContext context, String jobName, Int32 attempt, Boolean continueOnError, Int32 timeoutInMinutes, Int32 cancelTimeoutInMinutes, IJobFactory jobFactory);

        internal void Validate(
            IPipelineContext context,
            BuildOptions buildOptions,
            ValidationResult result)
        {
            this.Validate(context, buildOptions, result, new List<Step>(), new HashSet<Demand>());
        }

        internal abstract ExpandPhaseResult Expand(PhaseExecutionContext context, Boolean continueOnError, Int32 timeoutInMinutes, Int32 cancelTimeoutInMinutes, IJobFactory jobFactory, JobExpansionOptions options);

        internal virtual void Validate(
            IPipelineContext context,
            BuildOptions buildOptions,
            ValidationResult result,
            IList<Step> steps,
            ISet<Demand> taskDemands)
        {
        }

        internal JobExecutionContext CreateJobContext(
            PhaseExecutionContext context,
            String jobName,
            Int32 attempt,
            IJobFactory jobFactory)
        {
            var continueOnError = context.Evaluate(nameof(ContinueOnError), this.ContinueOnError, false).Value;
            var timeoutInMinutes = context.Evaluate(nameof(TimeoutInMinutes), this.TimeoutInMinutes, PipelineConstants.DefaultJobTimeoutInMinutes).Value;
            var cancelTimeoutInMinutes = context.Evaluate(nameof(CancelTimeoutInMinutes), this.CancelTimeoutInMinutes, PipelineConstants.DefaultJobCancelTimeoutInMinutes).Value;
            return this.CreateJobContext(context, jobName, attempt, continueOnError, timeoutInMinutes, cancelTimeoutInMinutes, jobFactory);
        }

        internal ExpandPhaseResult Expand(
            PhaseExecutionContext context,
            IJobFactory jobFactory,
            JobExpansionOptions options)
        {
            var continueOnError = context.Evaluate(nameof(ContinueOnError), this.ContinueOnError, false).Value;
            var timeoutInMinutes = context.Evaluate(nameof(TimeoutInMinutes), this.TimeoutInMinutes, PipelineConstants.DefaultJobTimeoutInMinutes).Value;
            var cancelTimeoutInMinutes = context.Evaluate(nameof(CancelTimeoutInMinutes), this.CancelTimeoutInMinutes, PipelineConstants.DefaultJobCancelTimeoutInMinutes).Value;
            return this.Expand(context, continueOnError, timeoutInMinutes, cancelTimeoutInMinutes, jobFactory, options);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_demands?.Count == 0)
            {
                m_demands = null;
            }
        }

        [DataMember(Name = "Demands", EmitDefaultValue = false)]
        private ISet<Demand> m_demands;
    }

    internal sealed class PhaseTargetJsonConverter : VssSecureJsonConverter
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
            return typeof(PhaseTarget).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
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

            PhaseTargetType? targetType = null;
            JObject value = JObject.Load(reader);
            if (!value.TryGetValue("Type", StringComparison.OrdinalIgnoreCase, out JToken targetTypeValue))
            {
                return existingValue;
            }
            else
            {
                if (targetTypeValue.Type == JTokenType.Integer)
                {
                    targetType = (PhaseTargetType)(Int32)targetTypeValue;
                }
                else if (targetTypeValue.Type == JTokenType.String)
                {
                    PhaseTargetType parsedType;
                    if (Enum.TryParse((String)targetTypeValue, ignoreCase: true, result: out parsedType))
                    {
                        targetType = parsedType;
                    }
                }
            }

            if (targetType == null)
            {
                return existingValue;
            }

            Object newValue = null;
            switch (targetType)
            {
                case PhaseTargetType.DeploymentGroup:
                    newValue = new DeploymentGroupTarget();
                    break;

                case PhaseTargetType.Server:
                    newValue = new ServerTarget();
                    break;

                case PhaseTargetType.Queue:
                    newValue = new AgentQueueTarget();
                    break;

                case PhaseTargetType.Pool:
                    newValue = new AgentPoolTarget();
                    break;
            }

            if (value != null)
            {
                using (JsonReader objectReader = value.CreateReader())
                {
                    serializer.Populate(objectReader, newValue);
                }
            }

            return newValue;
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
