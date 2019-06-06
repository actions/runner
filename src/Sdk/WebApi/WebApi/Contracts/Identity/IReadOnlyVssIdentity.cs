using System;

namespace GitHub.Services.Identity
{
    public interface IReadOnlyVssIdentity
    {
        Guid Id { get; }

        IdentityDescriptor Descriptor { get; }

        bool IsContainer { get; }

        bool IsExternalUser { get; }

        string DisplayName { get; }

        string ProviderDisplayName { get; }

        string CustomDisplayName { get; }

        TValue GetProperty<TValue>(string name, TValue defaultValue);
    }
}
