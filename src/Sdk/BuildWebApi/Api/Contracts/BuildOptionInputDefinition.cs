using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents an input for a build option.
    /// </summary>
    [DataContract]
    public class BuildOptionInputDefinition : BaseSecuredObject
    {
        public BuildOptionInputDefinition()
            : this(null)
        {
        }

        internal BuildOptionInputDefinition(
            ISecuredObject securedObject)
            : base(securedObject)
        {
            InputType = BuildOptionInputType.String;
            DefaultValue = String.Empty;
            Required = false;
        }

        /// <summary>
        /// The name of the input.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// The label for the input.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Label
        {
            get;
            set;
        }

        /// <summary>
        /// The default value.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DefaultValue
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the input is required to have a value.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean Required
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates the type of the input value.
        /// </summary>
        [DataMember(Name = "Type")]
        public BuildOptionInputType InputType
        {
            get;
            set;
        }

        /// <summary>
        /// The rule that is applied to determine whether the input is visible in the UI.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String VisibleRule
        {
            // Typical format is "NAMEOFTHEDEPENDENTINPUT = VALUETOBEBOUND"
            get;
            set;
        }

        /// <summary>
        /// The name of the input group that this input belongs to.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String GroupName
        {
            get;
            set;
        }

        /// <summary>
        /// A dictionary of options for this input.
        /// </summary>
        public Dictionary<String, String> Options
        {
            get
            {
                if (m_Options == null)
                {
                    m_Options = new Dictionary<String, String>();
                }
                return m_Options;
            }
            set
            {
                m_Options = value;
            }
        }

        /// <summary>
        /// A dictionary of help documents for this input.
        /// </summary>
        public Dictionary<String, String> HelpDocuments
        {
            get
            {
                if (m_HelpDocuments == null)
                {
                    m_HelpDocuments = new Dictionary<String, String>();
                }

                return m_HelpDocuments;
            }
            set
            {
                m_HelpDocuments = new Dictionary<String, String>(value);
            }
        }

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        private Dictionary<String, String> m_Options;

        [DataMember(Name = "Help", EmitDefaultValue = false)]
        private Dictionary<String, String> m_HelpDocuments;
    }
}
