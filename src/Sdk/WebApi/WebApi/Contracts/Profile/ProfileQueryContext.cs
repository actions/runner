using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
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
