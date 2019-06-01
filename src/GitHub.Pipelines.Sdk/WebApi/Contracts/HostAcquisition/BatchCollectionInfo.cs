// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.HostAcquisition
{
    [DataContract]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class BatchCollectionInfo
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public CollectionInfo Info { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String ExceptionType { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String ErrorMessage { get; set; }

        private string DebuggerDisplay
        {
            get { return $"{nameof(Id)}: {Id}"; }
        }
    }
}
