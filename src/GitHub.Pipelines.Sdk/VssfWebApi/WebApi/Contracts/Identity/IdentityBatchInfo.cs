using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Identity
{
    [DataContract]
    public class IdentityBatchInfo
    {
        private IdentityBatchInfo()
        {
        }

        public IdentityBatchInfo(IList<SubjectDescriptor> subjectDescriptors, QueryMembership queryMembership = QueryMembership.None, IEnumerable<string> propertyNames = null, bool includeRestrictedVisibility = false)
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(subjectDescriptors, nameof(subjectDescriptors));

            this.SubjectDescriptors = new List<SubjectDescriptor>(subjectDescriptors);
            this.QueryMembership = queryMembership;
            this.PropertyNames = propertyNames;
            this.IncludeRestrictedVisibility = includeRestrictedVisibility;
        }

        public IdentityBatchInfo(IList<IdentityDescriptor> descriptors, QueryMembership queryMembership = QueryMembership.None, IEnumerable<string> propertyNames = null, bool includeRestrictedVisibility = false)
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(descriptors, nameof(descriptors));

            this.Descriptors = new List<IdentityDescriptor>(descriptors);
            this.QueryMembership = queryMembership;
            this.PropertyNames = propertyNames;
            this.IncludeRestrictedVisibility = includeRestrictedVisibility;
        }

        public IdentityBatchInfo(IList<Guid> identityIds, QueryMembership queryMembership = QueryMembership.None, IEnumerable<string> propertyNames = null, bool includeRestrictedVisibility = false)
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(identityIds, nameof(identityIds));

            this.IdentityIds = new List<Guid>(identityIds);
            this.QueryMembership = queryMembership;
            this.PropertyNames = propertyNames;
            this.IncludeRestrictedVisibility = includeRestrictedVisibility;
        }

        public IdentityBatchInfo(IList<SocialDescriptor> socialDescriptors, QueryMembership queryMembership = QueryMembership.None, IEnumerable<string> propertyNames = null, bool includeRestrictedVisibility = false)
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(socialDescriptors, nameof(socialDescriptors));

            this.SocialDescriptors = new List<SocialDescriptor>(socialDescriptors);
            this.QueryMembership = queryMembership;
            this.PropertyNames = propertyNames;
            this.IncludeRestrictedVisibility = includeRestrictedVisibility;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<IdentityDescriptor> Descriptors { get; private set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<SubjectDescriptor> SubjectDescriptors { get; private set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<Guid> IdentityIds { get; private set; }

        [DataMember]
        public QueryMembership QueryMembership { get; private set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public IEnumerable<string> PropertyNames { get; private set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool IncludeRestrictedVisibility { get; private set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<SocialDescriptor> SocialDescriptors { get; private set; }
    }
}