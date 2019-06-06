// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;

namespace GitHub.Services.Identity
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public sealed class GroupMembership
    {
        public GroupMembership(Guid queriedId, Guid id, IdentityDescriptor descriptor)
        {
            QueriedId = queriedId;
            Id = id;
            Descriptor = descriptor;
            Active = true;
        }

        [DataMember]
        public Guid QueriedId
        {
            get;
            set;
        }

        [DataMember]
        public Guid Id
        {
            get
            {
                return m_id;
            }

            set
            {
                m_id = value;
            }
        }

        [DataMember]
        public IdentityDescriptor Descriptor
        {
            get;
            set;
        }

        [DataMember]
        public Boolean Active
        {
            get;
            set;
        }


        private Guid m_id;

        public GroupMembership Clone()
        {
            return new GroupMembership(
                queriedId: QueriedId, 
                id: Id, 
                descriptor: Descriptor == null ? null : new IdentityDescriptor(Descriptor))
            {
                Active = this.Active
            };
        }

        public override string ToString()
        {
            return string.Format("[Id = {0}, Descriptor = {1}, Active = {2}, QueriedId = {3}]", Id, Descriptor, Active, QueriedId);
        }
    }

    [CollectionDataContract(Name = "GroupMemberships", ItemName = "GroupMembership")]
    public class GroupMembershipCollection : List<GroupMembership>
    {
        public GroupMembershipCollection()
        {
        }

        public GroupMembershipCollection(IList<GroupMembership> source)
            : base(source)
        {
        }
    }
}
