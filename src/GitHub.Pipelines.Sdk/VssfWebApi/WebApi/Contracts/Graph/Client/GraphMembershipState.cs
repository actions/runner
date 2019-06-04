using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.Graph.Client
{
    /// <summary>
    /// Status of a Graph membership (active/inactive)
    /// </summary>
    [DataContract]
    public class GraphMembershipState
    {
        /// <summary>
        /// When true, the membership is active
        /// </summary>
        [DataMember]
        public bool Active { get; }

        /// <summary>
        /// This field contains zero or more interesting links about the graph membership state. 
        /// These links may be invoked to obtain additional relationships or more detailed 
        /// information about this graph membership state.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "_links")]
        public ReferenceLinks Links { get; private set; }

        public GraphMembershipState(bool active, ReferenceLinks links)
        {
            Active = active;
            Links = links;
        }
    }
}
