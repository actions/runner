namespace GitHub.Actions.WorkflowParser.Conversion
{
    internal sealed class PermissionLevelViolation
    {

        public PermissionLevelViolation(string permissionName, PermissionLevel requestedPermissions, PermissionLevel allowedPermissions)
        {
            PermissionName = permissionName;
            RequestedPermissionLevel = requestedPermissions;
            AllowedPermissionLevel = allowedPermissions;
        }

        public string PermissionName
        {
            get;
        }

        public PermissionLevel RequestedPermissionLevel
        {
            get;
        }
        public PermissionLevel AllowedPermissionLevel
        {
            get;
        }

        public string RequestedPermissionLevelString()
        {
            return $"{PermissionName}: {RequestedPermissionLevel.ConvertToString()}";
        }

        public string AllowedPermissionLevelString()
        {
            return $"{PermissionName}: {AllowedPermissionLevel.ConvertToString()}";
        }
    }
}