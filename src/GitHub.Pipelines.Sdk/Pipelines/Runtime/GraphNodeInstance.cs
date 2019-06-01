using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime
{
    [DataContract]
    public abstract class GraphNodeInstance<TNode> : IGraphNodeInstance where TNode : IGraphNode
    {
        private protected GraphNodeInstance()
        {
            this.Attempt = 1;
        }

        private protected GraphNodeInstance(
            String name,
            Int32 attempt,
            TNode definition,
            TaskResult result)
        {
            this.Name = name;
            this.Attempt = attempt;
            this.Definition = definition;
            this.State = PipelineState.NotStarted;
            this.Result = result;
        }

        /// <summary>
        /// Gets or sets the identifier of the node.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Identifier
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the node.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Int32 Attempt
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? StartTime
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? FinishTime
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the collection of output variables emitted by the instance.
        /// </summary>
        public IDictionary<String, VariableValue> Outputs
        {
            get
            {
                if (m_outputs == null)
                {
                    m_outputs = new VariablesDictionary();
                }
                return m_outputs;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public PipelineState State
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the result of the instance.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskResult? Result
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the structure defined in the pipeline definition.
        /// </summary>
        public TNode Definition
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the timeline record for this instance.
        /// </summary>
        internal TimelineRecord Record
        {
            get;
        }

        public Boolean SecretsAccessed
        {
            get
            {
                return m_outputs?.SecretsAccessed.Count > 0;
            }
        }

        public void ResetSecretsAccessed()
        {
            m_outputs?.SecretsAccessed.Clear();
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_outputs?.Count == 0)
            {
                m_outputs = null;
            }
        }

        [DataMember(Name = "Outputs", EmitDefaultValue = false)]
        private VariablesDictionary m_outputs;
    }
}
