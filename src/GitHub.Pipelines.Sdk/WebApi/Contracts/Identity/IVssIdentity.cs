using System;

namespace Microsoft.VisualStudio.Services.Identity
{
    public interface IVssIdentity : IReadOnlyVssIdentity
    {
        new IdentityDescriptor Descriptor { get; set; }

        new string ProviderDisplayName { get; set; }

        new string CustomDisplayName { get; set; }

        void SetProperty(string name, object value);
    }
}
