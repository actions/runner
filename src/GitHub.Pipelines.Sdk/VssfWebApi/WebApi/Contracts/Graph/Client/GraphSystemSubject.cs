using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using IdentityDescriptor = Microsoft.VisualStudio.Services.Identity.IdentityDescriptor;

namespace Microsoft.VisualStudio.Services.Graph.Client
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
