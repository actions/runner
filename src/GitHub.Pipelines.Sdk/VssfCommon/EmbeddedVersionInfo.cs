using System;
using System.Runtime.CompilerServices;

namespace GitHub.Services.Common
{
    [CompilerGenerated]
    internal static class GeneratedVersionInfo
    {
        // Legacy values which preserve semantics from prior to the Assembly / File version split.
        // See Toolsets\Version\Version.props for more details.
        public const String MajorVersion = "16";
        public const String MinorVersion = "0";
        public const String BuildVersion = "65000";
        public const String PatchVersion = "0";
        public const String ProductVersion = MajorVersion + "." + MinorVersion;

        // Assembly version (i.e. strong name)
        public const String AssemblyMajorVersion = "16";
        public const String AssemblyMinorVersion = "0";
        public const String AssemblyBuildVersion = "0";
        public const String AssemblyPatchVersion = "0";
        public const String AssemblyVersion = AssemblyMajorVersion + "." + AssemblyMinorVersion + "." + AssemblyBuildVersion + "." + AssemblyPatchVersion;

        // File version
        public const String FileMajorVersion = "16";
        public const String FileMinorVersion = "255";
        public const String FileBuildVersion = "65000";
        public const String FilePatchVersion = "0";
        public const String FileVersion = FileMajorVersion + "." + FileMinorVersion + "." + FileBuildVersion + "." + FilePatchVersion;

        // Derived versions
        public const String TfsMajorVersion = "8";
        public const String TfsMinorVersion = "0";
        public const String TfsProductVersion = TfsMajorVersion + "." + TfsMinorVersion;

        // On-premises TFS install folder
        public const String TfsInstallDirectory = "Azure DevOps Server 2019";
    }
}
