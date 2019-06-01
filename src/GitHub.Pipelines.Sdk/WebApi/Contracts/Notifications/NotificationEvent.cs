using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Notifications
{
    public enum EventSerializerType
    {
        Json,
        Xml,
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class NotificationEventBindingsAttribute : Attribute
    {
        public NotificationEventBindingsAttribute(EventSerializerType serializerType, String eventType)
        {
            SerializerType = serializerType;
            EventType = eventType;
        }

        public EventSerializerType SerializerType { get; }

        public String EventType { get; }

    }

    /// <summary>
    /// This is the type used for firing notifications intended for the subsystem in the Notifications SDK.
    /// For components that can't take a dependency on the Notifications SDK directly, they can use  
    /// ITeamFoundationEventService.PublishNotification and the Notifications SDK ISubscriber implementation
    /// will get it.
    /// </summary>
    [DataContract]
    public class VssNotificationEvent : ICloneable
    {
        public VssNotificationEvent()
        {
            m_createdTime = DateTime.UtcNow;
        }

        public VssNotificationEvent(Object data) : this()
        {
            InitFromObject(data);
        }

        public VssNotificationEvent(string serializedEvent, string eventType) : this()
        {
            Data = serializedEvent;
            EventType = eventType;
        }

        public void InitFromObject(Object data)
        {
            if (data is string)
            {
                throw new ArgumentException("Must use VssNotificationEvent(string, string) for already serialized events");
            }

            Data = data;

            Type type = data.GetType();

#if NOTIFICATIONS_EVENTS_USE_CONTRIBUTED_NAMES

            Type type = data.GetType();
            Object[] attributes = type.GetCustomAttributes(typeof(NotificationEventBindingsAttribute), true);

            if (attributes.Length > 0)
            {
                NotificationEventBindingsAttribute binding = (NotificationEventBindingsAttribute)attributes[0];
                EventType = binding.EventType;
            }
#else
            EventType = type.Name;
#endif
        }

        /// <summary>
        /// Required: The name of the event.  This event must be registered in the context it is being fired.
        /// </summary>
        [DataMember]
        public String EventType { get; set; }

        /// <summary>
        /// Required: The event payload.  If Data is a string, it must be in Json or XML format.  Otherwise it must have a 
        /// serialization format attribute.
        /// </summary>
        [DataMember]
        public Object Data { get; set; }

        /// <summary>
        /// Optional: A list of actors which are additional identities with corresponding roles that are relevant to 
        /// the event.
        /// </summary>
        [DataMember]
        public List<EventActor> Actors
        {
            get
            {
                if (null == m_actors)
                {
                    m_actors = new List<EventActor>();
                }
                return m_actors;
            }
        }

        /// <summary>
        /// Optional: A list of scopes which are are relevant to the event.
        /// </summary>
        [DataMember]
        public List<EventScope> Scopes
        {
            get
            {
                if (null == m_scopes)
                {
                    m_scopes = new List<EventScope>();
                }
                return m_scopes;
            }
        }

        /// <summary>
        /// Optional: A list of artifacts referenced or impacted by this event.
        /// </summary>
        [DataMember]
        public List<String> ArtifactUris
        {
            get
            {
                if (null == m_artifactUris)
                {
                    m_artifactUris = new List<String>();
                }
                return m_artifactUris;
            }
        }

        /// <summary>
        /// How long to wait before processing this event.  The default is to process immediately.
        /// </summary>
        [DataMember]
        public TimeSpan ProcessDelay { get; set; } = VssNotificationEvent.ProcessNow;

        /// <summary>
        /// How long before the event expires and will be cleaned up.  The default is to use the system default.
        /// </summary>
        [DataMember]
        public TimeSpan ExpiresIn { get; set; } = VssNotificationEvent.DefaultExpiration;

        /// <summary>
        /// This is the time the original source event for this VssNotificationEvent was created.  For example, for 
        /// something like a build completion notification SourceEventCreatedTime should be the time the build finished
        /// not the time this event was raised.
        /// </summary>
        [DataMember]
        public DateTime? SourceEventCreatedTime { get; set; }

        /// <summary>
        /// The id of the item, artifact, extension, project, etc.
        /// </summary>
        [DataMember]
        public String ItemId { get; set; }

        /// <summary>
        /// Quick check to see if there are any Actors to avoid creating the list during processing.
        /// </summary>
        public bool HasActors
        {
            get
            {
                return (null != m_actors) && (m_actors.Count > 0);
            }
        }

        /// <summary>
        /// Quick check to see if there are any Scopes to avoid creating the list during processing.
        /// </summary>
        public bool HasScopes
        {
            get
            {
                return (null != m_scopes) && (m_scopes.Count > 0);
            }
        }


        /// <summary>
        /// Quick check to see if there are any ArtifactUris to avoid creating the list during processing.
        /// </summary>
        public bool HasArtifactUris
        {
            get
            {
                return (null != m_artifactUris) && (m_artifactUris.Count > 0);
            }
        }

        public void AddActor(string role, Guid id)
        {
            Actors.Add(new EventActor() { Role = role, Id = id });
        }

        public void AddScope(string type, Guid id)
        {
            AddScope(type, id, null);
        }

        public void AddScope(string type, Guid id, String name)
        {
            Scopes.Add(new EventScope() { Type = type, Id = id, Name = name });
        }

        public void AddArtifactUri(String artificatUri)
        {
            ArtifactUris.Add(artificatUri);
        }

        public void AddSystemInitiatorActor()
        {
            AddActor(Roles.Initiator, KnownInitiators.System);
        }

        public Object Clone()
        {
            VssNotificationEvent that = new VssNotificationEvent();

            that.CloneFrom(this);

            return that;
        }

        protected virtual void CloneFrom(VssNotificationEvent other)
        {
            this.Actors.AddRange(other.Actors);
            this.ArtifactUris.AddRange(other.ArtifactUris);
            this.Data = other.Data;
            this.EventType = other.EventType;
            this.Scopes.AddRange(other.Scopes);
            this.ProcessDelay = other.ProcessDelay;
            this.ExpiresIn = other.ExpiresIn;
            this.SourceEventCreatedTime = other.SourceEventCreatedTime;
            this.ItemId = other.ItemId;
            this.m_createdTime = other.m_createdTime;
        }

        protected DateTime m_createdTime;

        private List<EventActor> m_actors;
        private List<EventScope> m_scopes;
        private List<String> m_artifactUris;

        public static readonly TimeSpan ProcessNow = TimeSpan.Zero;
        public static readonly TimeSpan DefaultExpiration = TimeSpan.MinValue;
        public static readonly TimeSpan NeverExpire = TimeSpan.MaxValue;

        public static class Roles
        {
            public static readonly String MainActor = "mainActor";
            public static readonly String Initiator = "initiator";
        }

        public static class ScopeNames
        {
            public static readonly String Project = "project";
            public static readonly String Repository = "repository";
        }

        public static class KnownInitiators
        {
            public static readonly Guid System = new Guid("00d7d880-a761-45b5-bca7-394bc63d0cc3");
        }
    }
}
