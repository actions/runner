using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// EnvironmentResourceReference.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class EnvironmentResourceReference
    {
        /// <summary>
        /// Id of the resource.
        /// </summary>
        [DataMember]
        public Int32 Id
        {
            get;
            set;
        }
        
        /// <summary>
        /// Name of the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Type of the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public EnvironmentResourceType Type
        {
            get;
            set;
        }

        /// <summary>
        /// List of linked resources
        /// </summary>
        public IList<EnvironmentLinkedResourceReference> LinkedResources
        {
            get
            {
                if (m_linkedResources == null)
                {
                    m_linkedResources = new List<EnvironmentLinkedResourceReference>();
                }

                return m_linkedResources;
            }
        }

        private IList<EnvironmentLinkedResourceReference> m_linkedResources;
    }
}
