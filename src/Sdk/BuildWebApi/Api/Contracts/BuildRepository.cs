using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a repository used by a build definition.
    /// </summary>
    [DataContract]
    public class BuildRepository : BaseSecuredObject
    {
        public BuildRepository()
        {
        }

        internal BuildRepository(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        private BuildRepository(
            BuildRepository toClone)
            : base(toClone)
        {
            this.Id = toClone.Id;
            this.Type = toClone.Type;
            this.Name = toClone.Name;
            this.Url = toClone.Url;
            this.DefaultBranch = toClone.DefaultBranch;
            this.RootFolder = toClone.RootFolder;
            this.Clean = toClone.Clean;
            this.CheckoutSubmodules = toClone.CheckoutSubmodules;

            if (toClone.m_properties != null)
            {
                foreach (var property in toClone.m_properties)
                {
                    this.Properties.Add(property.Key, property.Value);
                }
            }
        }

        /// <summary>
        /// The ID of the repository.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Id
        {
            get;
            set;
        }

        /// <summary>
        ///  The type of the repository.
        /// </summary>
        [DataMember]
        public String Type
        {
            get;
            set;
        }

        /// <summary>
        /// The friendly name of the repository.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// The URL of the repository.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri Url
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the default branch.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DefaultBranch
        {
            get;
            set;
        }

        /// <summary>
        /// The root folder.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String RootFolder
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether to clean the target folder when getting code from the repository.
        /// </summary>
        /// <remarks>
        /// This is a String so that it can reference variables.
        /// </remarks>
        [DataMember(EmitDefaultValue = true)]
        public String Clean
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether to checkout submodules.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Boolean CheckoutSubmodules
        {
            get;
            set;
        }

        /// <summary>
        /// A dictionary that holds additional information about the repository.
        /// </summary>
        public IDictionary<String, String> Properties
        {
            // Warning: This can contain secrets too. As part of #952656, we resolve secrets, it was done considering the fact that this is not a "DataMember"
            // If it's ever made a "DataMember" please be cautious, we would be leaking secrets
            get
            {
                if (m_properties == null)
                {
                    m_properties = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }

                return m_properties;
            }
            internal set
            {
                m_properties = new Dictionary<String, String>(value, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Clones this object.
        /// </summary>
        /// <returns></returns>
        public BuildRepository Clone()
        {
            return new BuildRepository(this);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedProperties, ref m_properties, StringComparer.OrdinalIgnoreCase, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_properties, ref m_serializedProperties, StringComparer.OrdinalIgnoreCase);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedProperties = null;
        }

        [DataMember(Name = "Properties", EmitDefaultValue = false)]
        private IDictionary<String, String> m_serializedProperties;

        // Warning: This can contain secrets too. As part of #952656, we resolve secrets, it was done considering the fact that this is not a "DataMember"
        // If it's ever made a "DataMember" please be cautious, we would be leaking secrets
        private IDictionary<String, String> m_properties;
    }
}
