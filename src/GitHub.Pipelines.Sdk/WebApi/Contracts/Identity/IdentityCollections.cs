using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Identity
{
    [CollectionDataContract(Name = "Identities", ItemName = "Identity")]
    public class IdentitiesCollection : List<Identity>
    {
        public IdentitiesCollection()
        {
        }

        public IdentitiesCollection(IList<Identity> source)
            : base(source)
        {
        }
    }

    [CollectionDataContract(Name = "Descriptors", ItemName = "Descriptor")]
    public class IdentityDescriptorCollection : List<IdentityDescriptor>
    {
        public IdentityDescriptorCollection()
        {
        }

        public IdentityDescriptorCollection(IList<IdentityDescriptor> source)
            : base(source)
        {
        }
    }

    [CollectionDataContract(Name = "IdentityIds", ItemName = "IdentityId")]
    public class IdentityIdCollection : List<Guid>
    {
        public IdentityIdCollection()
        {
        }

        public IdentityIdCollection(IList<Guid> source)
            : base(source)
        {
        }
    }
}
