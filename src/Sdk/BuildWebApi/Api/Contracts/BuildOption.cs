using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents the application of an optional behavior to a build definition.
    /// </summary>
    [DataContract]
    public class BuildOption : BaseSecuredObject
    {
        public BuildOption()
        {
        }

        internal BuildOption(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// A reference to the build option.
        /// </summary>
        [DataMember(IsRequired = true, Order = 1, Name = "Definition")]
        public virtual BuildOptionDefinitionReference BuildOptionDefinition
        {
            get;
            set;
        }

        /// <summary>
        /// The inputs that configure the behavior.
        /// </summary>
        public virtual IDictionary<String, String> Inputs
        {
            get
            {
                if (m_inputs == null)
                {
                    m_inputs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }

                return m_inputs;
            }
            internal set
            {
                m_inputs = new Dictionary<String, String>(value, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Indicates whether the behavior is enabled.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public virtual Boolean Enabled
        {
            get;
            set;
        }

        [DataMember(Name = "Inputs", EmitDefaultValue = false, Order = 2)]
        private Dictionary<String, String> m_inputs;
    }
}
