using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// This class describes a trace filter, i.e. a set of criteria on whether or not a trace event should be emitted
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class TraceFilter : IEquatable<TraceFilter>
    {
        [DataMember]
        public Guid TraceId { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public Int32 Tracepoint { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public String ProcessName { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public String UserLogin { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public String Service { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public String Method { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public TraceLevel Level { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public String Area { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public String Layer { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public String UserAgent { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public String Uri { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public String Path { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public Guid ServiceHost { get; set; }
        public String[] Tags { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public String ExceptionType { get; set; }
        [DataMember]
        public DateTime TimeCreated { get; set; }
        [DataMember]
        public bool IsEnabled { get; set; }
        [DataMember]
        public Guid OwnerId { get; set; }

        /// <summary>
        /// Used to serialize additional identity information (display name, etc) to clients.
        /// Not set by default. Server-side callers should use <see cref="OwnerId"/>.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef Owner { get; set; }

        // Fault Injection members (not part of the contract)
        public Exception ExceptionToInject { get; set; }
        public byte InjectionFrequency { get; set; }
        public TimeSpan DelayToInject { get; set; }

        public bool Equals(TraceFilter other)
        {
            return (other.TraceId == this.TraceId);
        }
    }
}
