using System;

namespace GitHub.Build.WebApi
{
    public static class BuildPermissions
    {
        public static readonly Int32 ViewBuilds = 1;
        public static readonly Int32 EditBuildQuality = 2;
        public static readonly Int32 RetainIndefinitely = 4;
        public static readonly Int32 DeleteBuilds = 8;
        public static readonly Int32 ManageBuildQualities = 16;
        public static readonly Int32 DestroyBuilds = 32;
        public static readonly Int32 UpdateBuildInformation = 64;
        public static readonly Int32 QueueBuilds = 128;
        public static readonly Int32 ManageBuildQueue = 256;
        public static readonly Int32 StopBuilds = 512;
        public static readonly Int32 ViewBuildDefinition = 1024;
        public static readonly Int32 EditBuildDefinition = 2048;
        public static readonly Int32 DeleteBuildDefinition = 4096;
        public static readonly Int32 OverrideBuildCheckInValidation = 8192;
        public static readonly Int32 AdministerBuildPermissions = 16384;

        public static readonly Int32 AllPermissions =
                ViewBuilds |
                EditBuildQuality |
                RetainIndefinitely |
                DeleteBuilds |
                ManageBuildQualities |
                DestroyBuilds |
                UpdateBuildInformation |
                QueueBuilds |
                ManageBuildQueue |
                StopBuilds |
                ViewBuildDefinition |
                EditBuildDefinition |
                DeleteBuildDefinition |
                OverrideBuildCheckInValidation |
                AdministerBuildPermissions;
    }
}
