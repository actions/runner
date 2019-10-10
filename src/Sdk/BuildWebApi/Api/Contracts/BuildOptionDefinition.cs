using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents an optional behavior that can be applied to a build definition.
    /// </summary>
    [DataContract]
    public class BuildOptionDefinition : BuildOptionDefinitionReference
    {
        public BuildOptionDefinition()
        {
        }

        internal BuildOptionDefinition(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// A value that indicates the relative order in which the behavior should be applied.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 Ordinal
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the build option.
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
        /// The list of inputs defined for the build option.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IList<BuildOptionInputDefinition> Inputs
        {
            get
            {
                if (m_inputs == null)
                {
                    m_inputs = new List<BuildOptionInputDefinition>();
                }

                return m_inputs;
            }
            set
            {
                m_inputs = new List<BuildOptionInputDefinition>(value);
            }
        }

        /// <summary>
        /// The list of input groups defined for the build option.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IList<BuildOptionGroupDefinition> Groups
        {
            get
            {
                if (m_groups == null)
                {
                    m_groups = new List<BuildOptionGroupDefinition>();
                }

                return m_groups;
            }
            set
            {
                m_groups = new List<BuildOptionGroupDefinition>(value);
            }
        }

        private List<BuildOptionInputDefinition> m_inputs;

        private List<BuildOptionGroupDefinition> m_groups;
    }
}
