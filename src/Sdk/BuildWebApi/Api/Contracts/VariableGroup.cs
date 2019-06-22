using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a variable group.
    /// </summary>
    [DataContract]
    public class VariableGroup : VariableGroupReference
    {
        public VariableGroup()
            : this(null)
        {
        }

        internal VariableGroup(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The type of the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Type
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the variable group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// The description.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        /// <summary>
        /// The variables in this group.
        /// </summary>
        public IDictionary<String, BuildDefinitionVariable> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new Dictionary<String, BuildDefinitionVariable>(StringComparer.OrdinalIgnoreCase);
                }

                return m_variables;
            }
            internal set
            {
                m_variables = new Dictionary<String, BuildDefinitionVariable>(value, StringComparer.OrdinalIgnoreCase);
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedVariables, ref m_variables, StringComparer.OrdinalIgnoreCase, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_variables, ref m_serializedVariables, StringComparer.OrdinalIgnoreCase);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedVariables = null;
        }

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private IDictionary<String, BuildDefinitionVariable> m_serializedVariables;

        private IDictionary<String, BuildDefinitionVariable> m_variables;
    }
}
