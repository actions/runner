using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Directories
{
    [GenerateSpecificConstants]
    public static class DirectoryEntityType
    {
        /// <summary>
        /// This concrete type implies that the directory entity represents a user.
        /// </summary>
        [GenerateConstant]
        public const string User = "User";

        /// <summary>
        /// This concrete type implies that the directory entity represents a group.
        /// </summary>
        [GenerateConstant]
        public const string Group = "Group";

        /// <summary>
        /// This supertype is used in directory entity descriptors and search requests to select any of the possible concrete types.
        /// </summary>
        public const string Any = "Any";
    }
}
