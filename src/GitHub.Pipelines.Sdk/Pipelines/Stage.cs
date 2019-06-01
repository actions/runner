using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Validation;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class Stage : IGraphNode
    {
        public Stage()
        {
        }

        public Stage(
            String name,
            IList<PhaseNode> phases)
        {
            this.Name = name;

            if (phases?.Count > 0)
            {
                m_phases = new List<PhaseNode>(phases);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String DisplayName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Condition
        {
            get;
            set;
        }

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

        public IList<PhaseNode> Phases
        {
            get
            {
                if (m_phases == null)
                {
                    m_phases = new List<PhaseNode>();
                }
                return m_phases;
            }
        }

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

        void IGraphNode.Validate(
            PipelineBuildContext context,
            ValidationResult result)
        {
            // Default the condition to something reasonable if none is specified
            if (String.IsNullOrEmpty(this.Condition))
            {
                this.Condition = StageCondition.Default;
            }
            else
            {
                // Simply construct the condition and make sure the syntax and functions used are valid
                var parsedCondition = new StageCondition(this.Condition);
            }

            if (m_variables?.Count > 0)
            {
                var variablesCopy = new List<IVariable>();
                foreach (var variable in this.Variables)
                {
                    if (variable is Variable simpleVariable)
                    {
                        // Do not allow phase overrides for certain variables. 
                        if (Phase.s_nonOverridableVariables.Contains(simpleVariable.Name))
                        {
                            continue;
                        }
                    }
                    else if (variable is VariableGroupReference groupVariable)
                    {
                        if (context.EnvironmentVersion < 2)
                        {
                            result.Errors.Add(new PipelineValidationError(PipelineStrings.StageVariableGroupNotSupported(this.Name, groupVariable)));
                            continue;
                        }

                        result.ReferencedResources.VariableGroups.Add(groupVariable);

                        if (context.BuildOptions.ValidateResources)
                        {
                            var variableGroup = context.ResourceStore.VariableGroups.Get(groupVariable);
                            if (variableGroup == null)
                            {
                                result.UnauthorizedResources.VariableGroups.Add(groupVariable);
                                result.Errors.Add(new PipelineValidationError(PipelineStrings.VariableGroupNotFoundForStage(this.Name, groupVariable)));
                            }
                        }
                    }

                    variablesCopy.Add(variable);
                }

                m_variables.Clear();
                m_variables.AddRange(variablesCopy);
            }

            GraphValidator.Validate(context, result, (input) => PipelineStrings.JobNameWhenNoNameIsProvided(input), this.Name, this.Phases, Phase.GetErrorMessage);
        }

        internal static String GetErrorMessage(
            String code,
            params Object[] values)
        {
            switch (code)
            {
                case PipelineConstants.NameInvalid:
                    // values[0] is the graph name which is null during stage graph validation
                    // values[1] is the stage name
                    return PipelineStrings.StageNameInvalid(values[1]);

                case PipelineConstants.NameNotUnique:
                    // values[0] is the graph name which is null during stage graph validation
                    // values[1] is the stage name
                    return PipelineStrings.StageNamesMustBeUnique(values[1]);

                case PipelineConstants.StartingPointNotFound:
                    return PipelineStrings.PipelineNotValidNoStartingStage();

                case PipelineConstants.DependencyNotFound:
                    // values[0] is the graph name which is null during stage graph validation
                    // values[1] is the node name
                    // values[2] is the dependency node name
                    return PipelineStrings.StageDependencyNotFound(values[1], values[2]);

                case PipelineConstants.GraphContainsCycle:
                    // values[0] is the graph name which is null during stage graph validation
                    // values[1] is the node name
                    // values[2] is the dependency node name
                    return PipelineStrings.StageGraphCycleDetected(values[1], values[2]);
            }

            throw new NotSupportedException();
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_dependsOn?.Count == 0)
            {
                m_dependsOn = null;
            }

            if (m_phases?.Count == 0)
            {
                m_phases = null;
            }

            if (m_variables?.Count == 0)
            {
                m_variables = null;
            }
        }

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private List<IVariable> m_variables;

        [DataMember(Name = "Phases", EmitDefaultValue = false)]
        private List<PhaseNode> m_phases;

        [DataMember(Name = "DependsOn", EmitDefaultValue = false)]
        private HashSet<String> m_dependsOn;
    }
}