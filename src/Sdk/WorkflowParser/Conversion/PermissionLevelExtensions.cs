using System;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    internal static class PermissionLevelExtensions
    {
        public static bool IsLessThanOrEqualTo(
            this PermissionLevel permissionLevel, 
            PermissionLevel other)
        {
            switch (permissionLevel, other)
            {
                case (PermissionLevel.NoAccess, PermissionLevel.NoAccess):
                case (PermissionLevel.NoAccess, PermissionLevel.Read):
                case (PermissionLevel.NoAccess, PermissionLevel.Write):
                case (PermissionLevel.Read, PermissionLevel.Read):
                case (PermissionLevel.Read, PermissionLevel.Write):
                case (PermissionLevel.Write, PermissionLevel.Write): 
                    return true;
                case (PermissionLevel.Read, PermissionLevel.NoAccess):
                case (PermissionLevel.Write, PermissionLevel.NoAccess):
                case (PermissionLevel.Write, PermissionLevel.Read):
                    return false;
                default:
                    throw new ArgumentException($"Invalid enum comparison: {permissionLevel} and {other}");
            }
        }

        public static string ConvertToString(this PermissionLevel permissionLevel)
        {
            switch (permissionLevel)
            {
                case PermissionLevel.NoAccess:
                    return "none";
                case PermissionLevel.Read:
                    return "read";
                case PermissionLevel.Write:
                    return "write";
                default:
                    throw new NotSupportedException($"invalid permission level found. {permissionLevel}");
            }
        }
    }
}