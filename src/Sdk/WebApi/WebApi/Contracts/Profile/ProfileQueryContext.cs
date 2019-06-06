using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    public class ProfileQueryContext
    {
        public ProfileQueryContext(AttributesScope scope, string containerName = null) 
            : this(scope, CoreProfileAttributes.All, containerName)
        {
        }

        public ProfileQueryContext(AttributesScope scope, CoreProfileAttributes coreAttributes, string containerName = null)
        {
            ContainerScope = scope;
            CoreAttributes = coreAttributes;
            switch (scope)
            {
            case AttributesScope.Core:
                ContainerName = null;
                break;
            case AttributesScope.Core | AttributesScope.Application:
                ProfileArgumentValidation.ValidateApplicationContainerName(containerName);
                ContainerName = containerName;
                break;
            default:
                throw new ArgumentException(string.Format("The scope '{0}' is not supported for this operation.", scope));
            }
        }

        [DataMember(IsRequired = true)]
        public AttributesScope ContainerScope { get; private set; }

        [DataMember]
        public string ContainerName { get; private set; }

        [DataMember]
        public CoreProfileAttributes CoreAttributes { get; private set; }
    }

    [Flags]
    public enum CoreProfileAttributes
    {
        Minimal           = 0x0000, // Does not contain email, avatar, display name, or marketing preferences
        Email             = 0x0001,
        Avatar            = 0x0002,
        DisplayName       = 0x0004,
        ContactWithOffers = 0x0008,
        All               = 0xFFFF,
    }
}
