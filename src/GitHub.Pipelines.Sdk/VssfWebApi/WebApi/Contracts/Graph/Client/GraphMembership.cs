using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Graph.Client
{
    /// <summary>
    /// Relationship between a container and a member
    /// </summary>
    [DataContract]
    public class GraphMembership
    {
        /// <summary>
        /// Descriptor of the container which the subject is a member of
        /// </summary>
        public SubjectDescriptor ContainerDescriptor { get; private set; }

        /// <summary>
        /// Descriptor of the subject which has membership in the container
        /// </summary>
        public SubjectDescriptor MemberDescriptor { get; private set; }

        [DataMember(Name = "ContainerDescriptor", IsRequired = false, EmitDefaultValue = false)]
        private string ContainerString
        {
            get { return ContainerDescriptor.ToString(); }
            set { ContainerDescriptor = SubjectDescriptor.FromString(value); }
        }

        [DataMember(Name = "MemberDescriptor", IsRequired = false, EmitDefaultValue = false)]
        private string MemberString
        {
            get { return MemberDescriptor.ToString(); }
            set { MemberDescriptor = SubjectDescriptor.FromString(value); }
        }

        /// <summary>
        /// This field contains zero or more interesting links about the graph membership. These
        /// links may be invoked to obtain additional relationships or more detailed information 
        /// about this graph membership.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "_links")]
        public ReferenceLinks Links { get; private set; }

        public GraphMembership(SubjectDescriptor memberDescriptor, SubjectDescriptor containerDescriptor, ReferenceLinks links)
        {
            MemberDescriptor = memberDescriptor;
            ContainerDescriptor = containerDescriptor;
            Links = links;
        }
    }
}
