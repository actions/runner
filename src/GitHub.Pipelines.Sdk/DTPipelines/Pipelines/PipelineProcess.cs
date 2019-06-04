using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineProcess : IOrchestrationProcess
    {
        [JsonConstructor]
        public PipelineProcess()
        {
        }

        public PipelineProcess(IList<PhaseNode> phases)
        {
            var stage = CreateDefaultStage();
            stage.Phases.AddRange(phases ?? Enumerable.Empty<PhaseNode>());
            this.Stages.Add(stage);
        }

        public PipelineProcess(IList<Stage> stages)
        {
            if (stages?.Count > 0)
            {
                m_stages = new List<Stage>(stages);
            }
        }

        public IList<Stage> Stages
        {
            get
            {
                if (m_stages == null)
                {
                    m_stages = new List<Stage>();
                }
                return m_stages;
            }
        }

        OrchestrationProcessType IOrchestrationProcess.ProcessType
        {
            get
            {
                return OrchestrationProcessType.Pipeline;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_stages?.Count == 1 && String.Equals(m_stages[0].Name, PipelineConstants.DefaultJobName, StringComparison.OrdinalIgnoreCase))
            {
                m_phases = new List<PhaseNode>(m_stages[0].Phases);
                m_stages = null;
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (m_phases?.Count > 0)
            {
                var stage = CreateDefaultStage();
                stage.Phases.AddRange(m_phases);

                m_phases = null;
                this.Stages.Add(stage);
            }
        }

        private static Stage CreateDefaultStage()
        {
            return new Stage { Name = PipelineConstants.DefaultJobName };
        }

        /// <summary>
        /// return the node at the given path
        /// </summary>
        public IGraphNode GetNodeAtPath(IList<String> path)
        {
            var length = path?.Count();
            var node = default(IGraphNode);
            if (length > 0)
            {
                // find stage
                node = this.Stages.FirstOrDefault(x => string.Equals(x.Name, path[0], StringComparison.OrdinalIgnoreCase)
                                                    || string.Equals(x.Name, PipelineConstants.DefaultJobName, StringComparison.OrdinalIgnoreCase));
                if (length > 1 && node != null)
                {
                    // find phase
                    node = (node as Stage).Phases.FirstOrDefault(x => string.Equals(x.Name, path[1], StringComparison.OrdinalIgnoreCase)
                                                                   || string.Equals(x.Name, PipelineConstants.DefaultJobName, StringComparison.OrdinalIgnoreCase));

                    // NOTE: jobs / phase configurations are not IGraphNodes yet
                }
            }

            return node;
        }

        [DataMember(Name = "Stages", EmitDefaultValue = false)]
        private List<Stage> m_stages;

        [DataMember(Name = "Phases", EmitDefaultValue = false)]
        private List<PhaseNode> m_phases;
    }
}
