using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.Common.Contracts
{
    [DataContract]
    public class ProcessParameters : BaseSecuredObject
    {
        public ProcessParameters()
            : this(null)
        {
        }

        public ProcessParameters(ISecuredObject securedObject)
            : this(null, securedObject)
        {
        }

        private ProcessParameters(ProcessParameters toClone, ISecuredObject securedObject)
            : base(securedObject)
        {
            if (toClone != null)
            {
                if (toClone.Inputs.Count > 0)
                {
                    Inputs.AddRange(toClone.Inputs.Select(i => i.Clone(securedObject)));
                }

                if (toClone.SourceDefinitions.Count > 0)
                {
                    SourceDefinitions.AddRange(toClone.SourceDefinitions.Select(sd => sd.Clone(securedObject)));
                }

                if (toClone.DataSourceBindings.Count > 0)
                {
                    DataSourceBindings.AddRange(toClone.DataSourceBindings.Select(dsb => dsb.Clone(securedObject)));
                }
            }
        }

        public IList<TaskInputDefinitionBase> Inputs
        {
            get
            {
                if (m_inputs == null)
                {
                    m_inputs = new List<TaskInputDefinitionBase>();
                }
                return m_inputs;
            }
        }

        public IList<TaskSourceDefinitionBase> SourceDefinitions
        {
            get
            {
                if (m_sourceDefinitions == null)
                {
                    m_sourceDefinitions = new List<TaskSourceDefinitionBase>();
                }
                return m_sourceDefinitions;
            }
        }

        public IList<DataSourceBindingBase> DataSourceBindings
        {
            get
            {
                if (m_dataSourceBindings == null)
                {
                    m_dataSourceBindings = new List<DataSourceBindingBase>();
                }
                return m_dataSourceBindings;
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var processParameters2 = obj as ProcessParameters;
            if (processParameters2 == null)
            {
                return false;
            }

            if (this.Inputs == null && processParameters2.Inputs == null)
            {
                return true;
            }

            if ((this.Inputs != null && processParameters2.Inputs == null)
                 || (this.Inputs == null && processParameters2.Inputs != null))
            { 
                return false;
            }
            
            if (this.Inputs.Count != processParameters2.Inputs.Count)
            {
                return false;
            }

            var orderedProcessParameters1 = this.Inputs.Where(i => i != null).OrderBy(i => i.Name);
            var orderedProcessParameters2 = processParameters2.Inputs.Where(i => i != null).OrderBy(i => i.Name);

            if (!orderedProcessParameters1.OrderBy(i => i.Name).SequenceEqual(orderedProcessParameters2))
            {
                return false;
            }

            return true;
        }

        public ProcessParameters Clone(ISecuredObject securedObject = null)
        {
            return new ProcessParameters(this, securedObject);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedInputs, ref m_inputs, true);
            SerializationHelper.Copy(ref m_serializedSourceDefinitions, ref m_sourceDefinitions, true);
            SerializationHelper.Copy(ref m_serializedDataSourceBindings, ref m_dataSourceBindings, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_inputs, ref m_serializedInputs);
            SerializationHelper.Copy(ref m_sourceDefinitions, ref m_serializedSourceDefinitions);
            SerializationHelper.Copy(ref m_dataSourceBindings, ref m_serializedDataSourceBindings);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedInputs = null;
            m_serializedSourceDefinitions = null;
            m_serializedDataSourceBindings = null;
        }

        [DataMember(Name = "Inputs", EmitDefaultValue = false)]
        private List<TaskInputDefinitionBase> m_serializedInputs;

        [DataMember(Name = "SourceDefinitions", EmitDefaultValue = false)]
        private List<TaskSourceDefinitionBase> m_serializedSourceDefinitions;

        [DataMember(Name = "DataSourceBindings", EmitDefaultValue = false)]
        private List<DataSourceBindingBase> m_serializedDataSourceBindings;

        private List<TaskInputDefinitionBase> m_inputs;
        private List<TaskSourceDefinitionBase> m_sourceDefinitions;
        private List<DataSourceBindingBase> m_dataSourceBindings;
    }
}
