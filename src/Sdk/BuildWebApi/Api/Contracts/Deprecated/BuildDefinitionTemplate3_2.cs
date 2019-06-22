using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Internals
{
    /// <summary>
    /// For back-compat with extensions that use the old Steps format instead of Process and Phases
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class BuildDefinitionTemplate3_2
    {

        public BuildDefinitionTemplate3_2()
        {
            Category = "Custom";
        }

        [DataMember(IsRequired = true)]
        public String Id
        {
            get;
            set;
        }

        [DataMember(IsRequired = true)]
        public String Name
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = true)]
        public Boolean CanDelete
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = true)]
        public String Category
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = true)]
        public String DefaultHostedQueue
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid IconTaskId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public BuildDefinition3_2 Template
        {
            get;
            set;
        }

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
        }

        [DataMember(EmitDefaultValue = false, Name = "Icons")]
        private Dictionary<String, String> m_icons;
    }

    internal static class BuildDefinitionTemplate3_2Extensions
    {
        public static BuildDefinitionTemplate ToBuildDefinitionTemplate(
            this BuildDefinitionTemplate3_2 source)
        {
            if (source == null)
            {
                return null;
            }

            var result = new BuildDefinitionTemplate()
            {
                CanDelete = source.CanDelete,
                Category = source.Category,
                DefaultHostedQueue = source.DefaultHostedQueue,
                Description = source.Description,
                IconTaskId = source.IconTaskId,
                Id = source.Id,
                Name = source.Name,
                Template = source.Template.ToBuildDefinition()
            };

            foreach (var iconPair in source.Icons)
            {
                result.Icons.Add(iconPair.Key, iconPair.Value);
            }

            return result;
        }
    }
}
