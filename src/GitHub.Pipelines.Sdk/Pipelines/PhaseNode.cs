using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Validation;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum PhaseType
    {
        [EnumMember]
        Phase,

        [EnumMember]
        Provider,

        [EnumMember]
        JobFactory,
    }

    [DataContract]
    [KnownType(typeof(Phase))]
    [KnownType(typeof(ProviderPhase))]
    [KnownType(typeof(JobFactory))]
    [JsonConverter(typeof(PhaseNodeJsonConverter))]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class PhaseNode : IGraphNode
    {
        protected PhaseNode()
        {
        }

        protected PhaseNode(PhaseNode nodeToCopy)
        {
            this.Name = nodeToCopy.Name;
            this.DisplayName = nodeToCopy.DisplayName;
            this.Condition = nodeToCopy.Condition;
            this.ContinueOnError = nodeToCopy.ContinueOnError;
            this.Target = nodeToCopy.Target?.Clone();

            if (nodeToCopy.m_dependsOn?.Count > 0)
            {
                m_dependsOn = new HashSet<String>(nodeToCopy.m_dependsOn, StringComparer.OrdinalIgnoreCase);
            }

            if (nodeToCopy.m_variables != null && nodeToCopy.m_variables.Count > 0)
            {
                m_variables = new List<IVariable>(nodeToCopy.m_variables);
            }
        }

        /// <summary>
        /// Gets the type of this phase.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public abstract PhaseType Type { get; }

        /// <summary>
        /// Gets or sets the name for this phase.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the display name for this phase.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a condition which is evaluated after all dependencies have been satisfied and determines
        /// whether or not the jobs within this phase should be executed or skipped.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Condition
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not failed jobs are considered a termination condition for
        /// the phase.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [JsonConverter(typeof(ExpressionValueJsonConverter<Boolean>))]
        public ExpressionValue<Boolean> ContinueOnError
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the set of phases which must complete before this phase begins execution.
        /// </summary>
        public ISet<String> DependsOn
        {
            get
            {
                if (m_dependsOn == null)
                {
                    m_dependsOn = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_dependsOn;
            }
        }

        /// <summary>
        /// Gets or sets the target for this phase.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public PhaseTarget Target
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the set of variables which will be provided to the phase steps.
        /// </summary>
        public IList<IVariable> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new List<IVariable>();
                }
                return m_variables;
            }
        }

        /// <summary>
        /// Resolves external references and ensures the steps are compatible with the selected target.
        /// </summary>
        /// <param name="context">The validation context</param>
        public virtual void Validate(
            PipelineBuildContext context,
            ValidationResult result)
        {
            // Ensure we have a target
            if (this.Target == null)
            {
                result.Errors.Add(new PipelineValidationError(PipelineStrings.PhaseTargetRequired(this.Name)));
                return;
            }

            if (this.Target.Type != PhaseTargetType.Queue && this.Target.Type != PhaseTargetType.Server && this.Target.Type != PhaseTargetType.Pool)
            {
                result.Errors.Add(new PipelineValidationError(PipelineStrings.UnsupportedTargetType(this.Name, this.Target.Type)));
                return;
            }

            // Default the condition to something reasonable if none is specified
            if (String.IsNullOrEmpty(this.Condition))
            {
                this.Condition = PhaseCondition.Default;
            }
            else
            {
                // Simply construct the condition and make sure the syntax and functions used are valid
                var parsedCondition = new PhaseCondition(this.Condition);
            }

            if (m_variables?.Count > 0)
            {
                var variablesCopy = new List<IVariable>();
                foreach (var variable in this.Variables)
                {
                    if (variable is Variable simpleVariable)
                    {
                        // Do not allow phase overrides for certain variables. 
                        if (s_nonOverridableVariables.Contains(simpleVariable.Name))
                        {
                            continue;
                        }
                    }
                    else if (variable is VariableGroupReference groupVariable)
                    {
                        if (context.EnvironmentVersion < 2)
                        {
                            result.Errors.Add(new PipelineValidationError(PipelineStrings.PhaseVariableGroupNotSupported(this.Name, groupVariable)));
                            continue;
                        }

                        result.ReferencedResources.VariableGroups.Add(groupVariable);

                        if (context.BuildOptions.ValidateResources)
                        {
                            var variableGroup = context.ResourceStore.VariableGroups.Get(groupVariable);
                            if (variableGroup == null)
                            {
                                result.UnauthorizedResources.VariableGroups.Add(groupVariable);
                                result.Errors.Add(new PipelineValidationError(PipelineStrings.VariableGroupNotFoundForPhase(this.Name, groupVariable)));
                            }
                        }
                    }

                    variablesCopy.Add(variable);
                }

                m_variables.Clear();
                m_variables.AddRange(variablesCopy);
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (m_dependencies?.Count > 0)
            {
                m_dependsOn = new HashSet<String>(m_dependencies.Select(x => x.Scope), StringComparer.OrdinalIgnoreCase);
                m_dependencies = null;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_dependsOn?.Count == 0)
            {
                m_dependsOn = null;
            }

            if (m_variables?.Count == 0)
            {
                m_variables = null;
            }
        }

        internal readonly static HashSet<String> s_nonOverridableVariables = new HashSet<String>(new[]
        {
            WellKnownDistributedTaskVariables.AccessTokenScope,
            WellKnownDistributedTaskVariables.JobParallelismTag
        }, StringComparer.OrdinalIgnoreCase);

        [JsonConverter(typeof(PhaseVariablesJsonConverter))]
        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private List<IVariable> m_variables;

        [DataMember(Name = "Dependencies", EmitDefaultValue = false)]
        private List<PhaseDependency> m_dependencies;

        [DataMember(Name = "DependsOn", EmitDefaultValue = false)]
        private HashSet<String> m_dependsOn;

        private class PhaseVariablesJsonConverter : JsonConverter
        {
            public PhaseVariablesJsonConverter()
            {
            }

            public override Boolean CanConvert(Type objectType)
            {
                return true;
            }

            public override Boolean CanWrite => true;

            public override Object ReadJson(
                JsonReader reader,
                Type objectType,
                Object existingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    return serializer.Deserialize<IList<IVariable>>(reader);
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    var dictionary = serializer.Deserialize<IDictionary<String, String>>(reader);
                    if (dictionary?.Count > 0)
                    {
                        return dictionary.Select(x => new Variable { Name = x.Key, Value = x.Value }).Cast<IVariable>().ToList();
                    }
                }

                return null;
            }

            public override void WriteJson(
                JsonWriter writer,
                Object value,
                JsonSerializer serializer)
            {
                var variables = value as IList<IVariable>;
                if (variables?.Count > 0)
                {
                    if (variables.Any(x => x is VariableGroupReference))
                    {
                        serializer.Serialize(writer, variables);
                    }
                    else
                    {
                        // This format is only here for back compat with the previous serialization format
                        var dictionary = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                        foreach (var variable in variables.OfType<Variable>())
                        {
                            dictionary[variable.Name] = variable.Value;
                        }

                        serializer.Serialize(writer, dictionary);
                    }
                }
            }
        }
    }

    internal sealed class PhaseNodeJsonConverter : VssSecureJsonConverter
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
            return typeof(PhaseNode).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
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

            PhaseType? phaseType = null;
            JObject value = JObject.Load(reader);
            if (!value.TryGetValue("Type", StringComparison.OrdinalIgnoreCase, out JToken phaseTypeValue))
            {
                phaseType = PhaseType.Phase;
            }
            else
            {
                if (phaseTypeValue.Type == JTokenType.Integer)
                {
                    phaseType = (PhaseType)(Int32)phaseTypeValue;
                }
                else if (phaseTypeValue.Type == JTokenType.String)
                {
                    PhaseType parsedType;
                    if (Enum.TryParse((String)phaseTypeValue, ignoreCase: true, result: out parsedType))
                    {
                        phaseType = parsedType;
                    }
                }
            }

            if (phaseType == null)
            {
                return existingValue;
            }

            Object newValue = null;
            switch (phaseType)
            {
                case PhaseType.Phase:
                    newValue = new Phase();
                    break;

                case PhaseType.Provider:
                    newValue = new ProviderPhase();
                    break;

                case PhaseType.JobFactory:
                    newValue = new JobFactory();
                    break;
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
            throw new NotImplementedException();
        }
    }
}
