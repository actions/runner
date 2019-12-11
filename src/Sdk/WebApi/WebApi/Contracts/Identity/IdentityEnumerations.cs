// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace GitHub.Services.Identity
{
    [DataContract]
    public enum GroupScopeType
    {
        [EnumMember, XmlEnum("0")]
        Generic = 0,

        [EnumMember, XmlEnum("1")]
        ServiceHost = 1,

        [EnumMember, XmlEnum("2")]
        TeamProject = 2
    }
}
