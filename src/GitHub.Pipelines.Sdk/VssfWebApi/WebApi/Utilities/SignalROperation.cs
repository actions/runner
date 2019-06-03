using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.Services.SignalR
{
    /// <summary>
    /// Containers of ISignalRObjects should implement this interface. If you implement this interface, all
    /// serializable properties must be of type ISignalRObject or IEnumerable of ISignalRObject. This will
    /// be enforced using a roslyn analyzer.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISignalRObjectContainer { }

    [DataContract]
    public sealed class SignalROperation : ISignalRObjectContainer
    {
        public List<SignalRObject> Operations
        {
            get
            {
                if (m_objects == null)
                {
                    m_objects = new List<SignalRObject>();
                }
                return m_objects;
            }
        }

        [DataMember(Name = "objects")]
        private List<SignalRObject> m_objects;
    }

    /// <summary>
    /// Complex types sent over SignalR should implement this interface. If you implement this interface, all
    /// serializable properties must be of type ISignalRObject or a primitive type. This will
    /// be enforced using a roslyn analyzer.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISignalRObject { }

    [DataContract]
    public sealed class SignalRObject : ISignalRObject
    {
        [DataMember]
        public string Identifier
        {
            get;
            set;
        }

        [DataMember]
        public string Version
        {
            get;
            set;
        }
    }
}
