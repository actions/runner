// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.HostAcquisition
{
    [DataContract]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class CollectionInfo
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public String Name { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Url { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String OwnerSignInAddress { get; set; }

        private string DebuggerDisplay
        {
            get { return $"{nameof(Name)}: {Name}"; }
        }
    }
}
