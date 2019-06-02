using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Graph.Client
{
    [DataContract]
    public class GraphMembershipTraversal
    {
        /// <summary>
        /// The traversed subject descriptor
        /// </summary>
        [DataMember]
        public SubjectDescriptor SubjectDescriptor { get; set; }

        /// <summary>
        /// When true, the subject is traversed completely
        /// </summary>
        [DataMember]
        public bool IsComplete { get; set; }

        /// <summary>
        /// Reason why the subject could not be traversed completely
        /// </summary>
        [DataMember]
        public string IncompletenessReason { get; set; }

        /// <summary>
        /// Subject descriptors of the traversed members
        /// </summary>
        [DataMember]
        public IEnumerable<SubjectDescriptor> TraversedSubjects { get; set; }

        /// <summary>
        /// Subject descriptor ids of the traversed members
        /// </summary>
        [DataMember]
        internal IEnumerable<Guid> TraversedSubjectIds { get; set; }
    }
}
