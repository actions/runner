using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.Services.Identity
{
    /// <summary>
    /// Container class for changed identities
    /// </summary>
    [DataContract]
    public class ChangedIdentities
    {
        [JsonConstructor]
        private ChangedIdentities()
        {
        }

        public ChangedIdentities(
            IList<Identity> identities,
            ChangedIdentitiesContext sequenceContext) :
            this(identities, sequenceContext, false)
        {
        }

        public ChangedIdentities(
            IList<Identity> identities,
            ChangedIdentitiesContext sequenceContext,
            bool moreData)
        {
            Identities = identities;
            SequenceContext = sequenceContext;
            MoreData = moreData;
        }

        /// <summary>
        /// Changed Identities
        /// </summary>
        [DataMember]
        public IList<Identity> Identities { get; private set; }

        /// <summary>
        /// Last Identity SequenceId
        /// </summary>
        [DataMember]
        public ChangedIdentitiesContext SequenceContext { get; private set; }

        /// <summary>
        /// More data available, set to true if pagesize is specified.
        /// </summary>
        [DataMember]
        public bool MoreData { get; private set; }
    }

    /// <summary>
    /// Context class for changed identities
    /// </summary>
    [DataContract]
    public class ChangedIdentitiesContext
    {
        [JsonConstructor]
        private ChangedIdentitiesContext()
        {
        }

        public ChangedIdentitiesContext(
            Int32 identitySequenceId,
            Int32 groupSequenceId) :
            this(identitySequenceId, groupSequenceId, ChangedIdentitiesContext.UnspecifiedSequenceId)
        {
        }

        public ChangedIdentitiesContext(
            Int32 identitySequenceId,
            Int32 groupSequenceId,
            Int32 organizationIdentitySequenceId) :
            this(identitySequenceId, groupSequenceId, organizationIdentitySequenceId, 0)
        {
        }

        public ChangedIdentitiesContext(
            Int32 identitySequenceId,
            Int32 groupSequenceId,
            Int32 organizationIdentitySequenceId,
            Int32 pageSize)
        {
            IdentitySequenceId = identitySequenceId;
            GroupSequenceId = groupSequenceId;
            OrganizationIdentitySequenceId = organizationIdentitySequenceId;
            PageSize = pageSize;
        }

        /// <summary>
        /// Last Identity SequenceId
        /// </summary>
        [DataMember]
        public Int32 IdentitySequenceId { get; private set; }

        /// <summary>
        /// Last Group SequenceId
        /// </summary>
        [DataMember]
        public Int32 GroupSequenceId { get; private set; }

        /// <summary>
        /// Last Group OrganizationIdentitySequenceId
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 OrganizationIdentitySequenceId { get; private set; }

        /// <summary>
        /// Page size
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 PageSize { get; private set; }

        private static int UnspecifiedSequenceId = -1;
    }
}
