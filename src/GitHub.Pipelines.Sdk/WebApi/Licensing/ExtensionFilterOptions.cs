using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Licensing
{
    [Flags]
    public enum ExtensionFilterOptions
    {
        // Only the users that are eligible for the extension and don't have it through any assignment
        [EnumMember]
        None = 1,

        // Only the users that have the extension through their bundles
        [EnumMember]
        Bundle = 2,

        // Only the users that have the extension through account assignment
        [EnumMember]
        AccountAssignment = 4,

        // Only the users that have the extension through Implicit assignment
        [EnumMember]
        ImplicitAssignment = 8,

        // combination of all filters above, i.e., all users that are elgible for the extension
        [EnumMember]
        All = -1
    }
}
