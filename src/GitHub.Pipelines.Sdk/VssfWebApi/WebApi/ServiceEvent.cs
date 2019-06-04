using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace GitHub.Services.WebApi
{
    // This is the event that shall be published on the service bus by different services for other first party services.
    [DataContract]
    public class ServiceEvent
    {
        private Object m_resource;

        /// <summary>
        /// This is the id of the type. 
        /// Constants that will be used by subscribers to identify/filter events being published on a topic.
        /// </summary>
        [DataMember]
        public String EventType { get; set; }

        /// <summary>
        /// This is the service that published this event.
        /// </summary>
        [DataMember]
        public Publisher Publisher { get; set; }

        /// <summary>
        /// The resource object that carries specific information about the event. The object must have
        /// the ServiceEventObject applied for serialization/deserialization to work.
        /// </summary>
        [DataMember]
        public Object Resource
        {
            get
            {
                return m_resource;
            }
            set
            {
                Type type = value.GetType();
                if (!type.GetTypeInfo().GetCustomAttributes<ServiceEventObjectAttribute>(true).Any())
                {
                    throw new InvalidOperationException($"Resource of type {type.FullName} must have ServiceEventObject attribute");
                }
                m_resource = value;
            }
        }

        /// <summary>
        /// This is the version of the resource. 
        /// </summary>
        [DataMember]
        public String ResourceVersion { get; set; }

        /// <summary>
        /// This dictionary carries the context descriptors along with their ids.
        /// </summary>
        [DataMember]
        public Dictionary<String, Object> ResourceContainers { get; set; }
    }

    [DataContract]
    public class Publisher
    {
        /// <summary>
        /// Name of the publishing service.
        /// </summary>
        [DataMember]
        public String Name { get; set; }

        /// <summary>
        /// Service Owner Guid
        /// Eg. Tfs : 00025394-6065-48CA-87D9-7F5672854EF7
        /// </summary>
        [DataMember]
        public Guid ServiceOwnerId { get; set; }
    }

    public class ResourceContainerTypes
    {
        public const String Account = "Account";
        public const String Collection = "Collection";
    }

    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true)]
    public class ServiceEventObjectAttribute : Attribute
    {
        public ServiceEventObjectAttribute() { }
    }
}
