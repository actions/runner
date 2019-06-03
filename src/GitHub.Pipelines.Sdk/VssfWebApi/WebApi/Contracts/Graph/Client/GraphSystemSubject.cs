using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using IdentityDescriptor = GitHub.Services.Identity.IdentityDescriptor;

namespace GitHub.Services.Graph.Client
{
    [DataContract]
    public class GraphSystemSubject : GraphSubject
    {
        public override string SubjectKind => Constants.SubjectKind.SystemSubject;

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal GraphSystemSubject(
            string origin,
            string originId,
            SubjectDescriptor descriptor,
            IdentityDescriptor legacyDescriptor,
            string displayName,
            ReferenceLinks links,
            string url)
            : base(origin, originId, descriptor, legacyDescriptor, displayName, links, url)
        {
        }

        // only for serialization
        protected GraphSystemSubject() { }
    }
}
