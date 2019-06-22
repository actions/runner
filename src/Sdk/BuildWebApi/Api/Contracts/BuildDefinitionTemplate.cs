using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a template from which new build definitions can be created.
    /// </summary>
    [DataContract]
    public class BuildDefinitionTemplate
    {

        public BuildDefinitionTemplate()
        {
            Category = "Custom";
        }

        /// <summary>
        /// The ID of the template.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Id
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the template.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the template can be deleted.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Boolean CanDelete
        {
            get;
            set;
        }

        /// <summary>
        /// The template category.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public String Category
        {
            get;
            set;
        }

        /// <summary>
        /// An optional hosted agent queue for the template to use by default.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public String DefaultHostedQueue
        {
            get;
            set;
        }

        /// <summary>
        /// The ID of the task whose icon is used when showing this template in the UI.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid IconTaskId
        {
            get;
            set;
        }

        /// <summary>
        /// A description of the template.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        /// <summary>
        /// The actual template.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public BuildDefinition Template
        {
            get;
            set;
        }

        /// <summary>
        /// A dictionary of media type strings to icons for this template.
        /// </summary>
        public IDictionary<String, String> Icons
        {
            get
            {
                if (m_icons == null)
                {
                    m_icons = new Dictionary<String, String>(StringComparer.Ordinal);
                }

                return m_icons;
            }
            internal set
            {
                m_icons = new Dictionary<String, String>(value, StringComparer.Ordinal);
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "Icons")]
        private Dictionary<String, String> m_icons;
    }
}
