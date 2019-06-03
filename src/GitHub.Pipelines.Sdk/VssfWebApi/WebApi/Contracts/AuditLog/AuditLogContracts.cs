using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.Notifications;
using GitHub.Services.WebApi;

namespace GitHub.Services.Audit
{
    /// <summary>
    /// The type of scope from where the event is originated
    /// </summary>
    [Flags]
    public enum AuditScopeType
    {
        /// <summary>
        /// The scope is not known or has not been set
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Deployment
        /// </summary>
        Deployment = 1,

        /// <summary>
        /// Enterprise
        /// </summary>
        Enterprise = 2,

        /// <summary>
        /// Organization
        /// </summary>
        Organization = 4,

        /// <summary>
        /// Project
        /// </summary>
        Project = 8
    }

    [DataContract]
    [NotificationEventBindings(EventSerializerType.Json, AuditLogNotificationTypes.AuditEvent)]
    public class AuditLogEntry
    {
        /// <summary>
        /// EventId, should be unique
        /// </summary>
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// This allows us to group things together, like one user action that caused a cascade of event entries (project creation).
        /// </summary>
        [DataMember]
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// ActivityId
        /// </summary>
        [DataMember]
        public Guid ActivityId { get; set; }

        /// <summary>
        /// The Actor's CUID
        /// </summary>
        [DataMember]
        public Guid ActorCUID { get; set; }

        /// <summary>
        /// The Actor's User Id
        /// </summary>
        [DataMember]
        public Guid ActorUserId { get; set; }

        /// <summary>
        /// Type of authentication used by the author
        /// </summary>
        [DataMember]
        public string AuthenticationMechanism { get; set; }

        /// <summary>
        /// The time when the event occurred in UTC
        /// </summary>
        [DataMember]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The type of the scope, Enterprise, Organization or Project
        /// </summary>
        [DataMember]
        public AuditScopeType ScopeType { get; set; }

        /// <summary>
        /// The org, collection or project Id
        /// </summary>
        [DataMember]
        public Guid ScopeId { get; set; }

        /// <summary>
        /// IP Address where the event was originated
        /// </summary>
        [DataMember]
        public String IPAddress { get; set; }

        /// <summary>
        /// The user agent from the request
        /// </summary>
        [DataMember]
        public String UserAgent { get; set; }

        /// <summary>
        /// The action if for the event, i.e Git.CreateRepo, Project.RenameProject
        /// </summary>
        [DataMember]
        public String ActionId { get; set; }

        /// <summary>
        /// External data such as CUIDs, item names, etc.
        /// </summary>
        [DataMember]
        public IDictionary<String, object> Data { get; set; }

        /// <summary>
        /// Returns a string representation of the object
        /// </summary>
        public override string ToString() => JsonUtility.ToString(this);
    }

    public static class AuditLogNotificationTypes
    {
        public const string AuditEvent = "ms.vss-notifications.audit-event";
        public const string AuditImplicitSubscription = "ms.vss-notifications.audit-event-implicit-subscription";
        public const string AuditPublisher = "ms.vss-notifications.audit-event-publisher";
    }
}
