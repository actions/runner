using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents resources used by a build process.
    /// </summary>
    [DataContract]
    public sealed class BuildProcessResources : BaseSecuredObject
    {
        public BuildProcessResources()
        {
        }

        internal BuildProcessResources(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// Information about the queues used by the process.
        /// </summary>
        public IList<AgentPoolQueueReference> Queues
        {
            get
            {
                if (m_queues == null)
                {
                    m_queues = new List<AgentPoolQueueReference>();
                }
                return m_queues;
            }
            set
            {
                m_queues = new List<AgentPoolQueueReference>(value);
            }
        }

        /// <summary>
        /// Information about the endpoints used by the process.
        /// </summary>
        public IList<ServiceEndpointReference> Endpoints
        {
            get
            {
                if (m_endpoints == null)
                {
                    m_endpoints = new List<ServiceEndpointReference>();
                }
                return m_endpoints;
            }
            set
            {
                m_endpoints = new List<ServiceEndpointReference>(value);
            }
        }

        /// <summary>
        /// Information about the secure files used by the process.
        /// </summary>
        public IList<SecureFileReference> Files
        {
            get
            {
                if (m_files == null)
                {
                    m_files = new List<SecureFileReference>();
                }
                return m_files;
            }
            set
            {
                m_files = new List<SecureFileReference>(value);
            }
        }

        /// <summary>
        /// Information about the variable groups used by the process.
        /// </summary>
        public IList<VariableGroupReference> VariableGroups
        {
            get
            {
                if (m_variableGroups == null)
                {
                    m_variableGroups = new List<VariableGroupReference>();
                }
                return m_variableGroups;
            }
            set
            {
                m_variableGroups = new List<VariableGroupReference>(value);
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_queues?.Count == 0)
            {
                m_queues = null;
            }

            if (m_endpoints?.Count == 0)
            {
                m_endpoints = null;
            }

            if (m_files?.Count == 0)
            {
                m_files = null;
            }

            if (m_variableGroups?.Count == 0)
            {
                m_variableGroups = null;
            }
        }

        [DataMember(Name = "Queues", EmitDefaultValue = false)]
        private List<AgentPoolQueueReference> m_queues;

        [DataMember(Name = "Endpoints", EmitDefaultValue = false)]
        private List<ServiceEndpointReference> m_endpoints;

        [DataMember(Name = "Files", EmitDefaultValue = false)]
        private List<SecureFileReference> m_files;

        [DataMember(Name = "VariableGroups", EmitDefaultValue = false)]
        private List<VariableGroupReference> m_variableGroups;
    }

    /// <summary>
    /// Represents a reference to a resource.
    /// </summary>
    [DataContract]
    public abstract class ResourceReference : BaseSecuredObject
    {
        public ResourceReference()
        {
        }

        protected ResourceReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// An alias to be used when referencing the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Alias
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Represents a reference to an agent queue.
    /// </summary>
    [DataContract]
    public class AgentPoolQueueReference : ResourceReference
    {
        public AgentPoolQueueReference()
            : this(null)
        {
        }

        internal AgentPoolQueueReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The ID of the queue.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 Id
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Represents a referenec to a service endpoint.
    /// </summary>
    [DataContract]
    public class ServiceEndpointReference : ResourceReference
    {
        public ServiceEndpointReference()
            : this(null)
        {
        }

        internal ServiceEndpointReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The ID of the service endpoint.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid Id
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Represents a reference to a secure file.
    /// </summary>
    [DataContract]
    public class SecureFileReference : ResourceReference
    {
        public SecureFileReference()
            : this(null)
        {
        }

        internal SecureFileReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The ID of the secure file.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid Id
        {
            get;
            set;
        }
    }
}
